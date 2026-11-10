using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

public record FechaProximaInput(DateTime? Fecha);

// Requisito TOEFL del alumno (Figma T1-T2): registra la fecha del examen y sube el comprobante (-> Presentado).
// El coordinador lo resuelve desde la ficha (AlumnosController).
[ApiController, Route("api/toefl"), Authorize(Roles = Roles.Alumno)]
public class ToeflController(TgaDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    Task<int> ClaveAlumno()
    {
        var id = User.IdUsuario();
        return db.Alumno.Where(a => a.IdUsuario == id).Select(a => a.ClaveAlumno).SingleAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Consultar()
    {
        var clave = await ClaveAlumno();
        var t = await db.RequisitoToefl.AsNoTracking().Where(x => x.ClaveAlumno == clave).OrderByDescending(x => x.IdToefl)
            .Select(x => new { Estado = x.IdEstadoToeflNavigation!.Nombre, x.FechaPresentacion, x.Puntaje, x.FechaProxima }).FirstOrDefaultAsync();
        var estado = t?.Estado ?? "Pendiente";
        // En Pendiente (sin presentar o no aprobado) se muestra el formulario vacío y, si la registró, la fecha en que lo presentará.
        if (estado == "Pendiente") return Ok(new { estado, fechaProxima = t?.FechaProxima });
        var comprobante = await db.Documento.AsNoTracking()
            .Where(d => d.ClaveAlumno == clave && d.IdTipoDocumento == TipoDocumento.ComprobanteToefl)
            .OrderByDescending(d => d.FechaCarga).Select(d => new { id = d.IdDocumento, nombre = d.NombreArchivo }).FirstOrDefaultAsync();
        return Ok(new { estado, fechaPresentacion = t!.FechaPresentacion, puntaje = t.Puntaje, comprobante });
    }

    // Fecha en que el alumno presentará el examen (D4). Al pasar, la pantalla le pide subir resultados o cambiarla.
    [HttpPut("fecha-proxima")]
    public async Task<IActionResult> FechaProxima(FechaProximaInput input)
    {
        if (input.Fecha is null) return BadRequest(Error("Indica la fecha del examen."));
        if (input.Fecha.Value.Date < DateTime.Today) return BadRequest(Error("La fecha no puede ser pasada."));

        var clave = await ClaveAlumno();
        var t = await db.RequisitoToefl.Where(x => x.ClaveAlumno == clave).OrderByDescending(x => x.IdToefl).FirstOrDefaultAsync();
        if (t is not null && t.IdEstadoToefl != EstadoToefl.Pendiente) return Conflict(Error("Tu examen ya está registrado."));
        if (t is null) db.RequisitoToefl.Add(t = new Models.RequisitoToefl { ClaveAlumno = clave, IdEstadoToefl = EstadoToefl.Pendiente });
        t.FechaProxima = input.Fecha.Value.Date;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost, RequestSizeLimit(DocumentosController.LimitePeticion)]
    public async Task<IActionResult> Registrar([FromForm] DateTime? fecha, IFormFile? archivo)
    {
        if (fecha is null) return BadRequest(Error("Indica la fecha del examen."));
        if (fecha.Value.Date > DateTime.Today) return BadRequest(Error("La fecha del examen no puede ser futura."));
        if (await DocumentosController.ValidarPdf(archivo, "Selecciona el comprobante en PDF.") is { } invalido) return BadRequest(Error(invalido));

        var clave = await ClaveAlumno();
        await using var tx = await db.Database.BeginTransactionAsync();
        var t = await db.RequisitoToefl.Where(x => x.ClaveAlumno == clave).OrderByDescending(x => x.IdToefl).FirstOrDefaultAsync();
        if (t is not null && t.IdEstadoToefl != EstadoToefl.Pendiente)
            return Conflict(Error("Tu examen ya está registrado."));
        if (t is null) db.RequisitoToefl.Add(t = new Models.RequisitoToefl { ClaveAlumno = clave });
        t.IdEstadoToefl = EstadoToefl.Presentado;
        t.FechaPresentacion = fecha.Value.Date;
        t.Puntaje = null;
        t.Observaciones = null;
        t.FechaProxima = null;

        var (enDisco, ruta, nombre) = await DocumentosController.GuardarPdf(archivo!, config, env);
        db.Documento.Add(new Models.Documento
        {
            ClaveAlumno = clave,
            IdTipoDocumento = TipoDocumento.ComprobanteToefl,
            NombreArchivo = nombre,
            RutaArchivo = enDisco,
            FechaCarga = DateTime.UtcNow,
            IdUsuario = User.IdUsuario(),
        });
        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }
        return NoContent();
    }
}
