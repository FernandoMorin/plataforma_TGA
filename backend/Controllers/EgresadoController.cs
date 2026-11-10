using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record PerfilEgresadoInput([Required, EmailAddress, MaxLength(100)] string Correo, [MaxLength(20)] string? Telefono, [MaxLength(100)] string? Ciudad, [MaxLength(255)] string? Otros);
public record EmpleoInput(
    [Required, MaxLength(150)] string Empresa, [Required, MaxLength(100)] string Puesto, [MaxLength(100)] string? Area, [Required] string Tipo,
    DateTime Inicio, DateTime? Fin, bool Relacion, [Range(0, 99_999_999)] decimal? Sueldo);
public record ComentarioInput([MaxLength(4000)] string? Comentario, [MaxLength(4000)] string? Sugerencia);

// Portal del egresado (Figma G1-G5). El egresado se identifica por usuario.clave_institucional = identificador de egresado.
[ApiController, Route("api/egresado"), Authorize(Roles = Roles.Egresado)]
public class EgresadoController(TgaDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    async Task<Egresado> Yo()
    {
        var id = User.IdUsuario();
        var identificador = await db.Usuario.Where(u => u.IdUsuario == id).Select(u => u.ClaveInstitucional).SingleAsync();
        return await db.Egresado.SingleAsync(e => e.IdentificadorEgresado == identificador);
    }

    // G1
    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        var e = await Yo();
        var a = await db.Alumno.AsNoTracking().Where(x => x.ClaveAlumno == e.ClaveAlumno)
            .Select(x => new { x.Nombre, x.ApellidoPaterno, x.ApellidoMaterno, Generacion = x.IdGeneracionNavigation!.NombreGeneracion }).SingleAsync();
        return Ok(new
        {
            identificador = e.IdentificadorEgresado, generacion = a.Generacion, nombre = Titulacion.NombreCompleto(a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno),
            correo = e.Correo, telefono = e.Telefono, ciudad = e.Ciudad, otros = e.OtrosContactos, actualizado = e.UltimaActualizacion,
            debeActualizar = Egresados.DebeActualizar(e.UltimaActualizacion),
        });
    }

    // Guardar o confirmar sin cambios: ambos renuevan la fecha de actualización; quien responde vuelve a ser localizable.
    [HttpPut("perfil")]
    public async Task<IActionResult> Guardar(PerfilEgresadoInput input)
    {
        var e = await Yo();
        var correo = input.Correo.Trim().ToLowerInvariant();
        var id = User.IdUsuario();
        var usuario = await db.Usuario.SingleAsync(u => u.IdUsuario == id);
        if (correo != usuario.Correo && await db.Usuario.AnyAsync(u => u.Correo == correo)) return Conflict(Error("Ese correo ya tiene una cuenta."));
        usuario.Correo = correo; // es su usuario para entrar
        e.Correo = correo;
        e.Telefono = Limpio(input.Telefono);
        e.Ciudad = Limpio(input.Ciudad);
        e.OtrosContactos = Limpio(input.Otros);
        Renovar(e);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("perfil/confirmar")]
    public async Task<IActionResult> Confirmar()
    {
        Renovar(await Yo());
        await db.SaveChangesAsync();
        return NoContent();
    }

    static void Renovar(Egresado e)
    {
        e.UltimaActualizacion = DateTime.Today;
        e.IdEstadoEgresado = Egresados.Localizable;
    }

    static string? Limpio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // G2
    [HttpGet("empleos")]
    public async Task<IActionResult> Empleos()
    {
        var e = await Yo();
        var empleos = await Egresados.Empleos(db, e.ClaveAlumno).ToListAsync();
        return Ok(empleos.OrderByDescending(x => x.Vigente).ThenByDescending(x => x.Inicio).Select(x => new
        {
            id = x.Id, tipo = x.Tipo, puesto = x.Puesto, empresa = x.Empresa, area = x.Area, inicio = x.Inicio, fin = x.Fin,
            relacion = x.Relacion, sueldo = x.Sueldo, vigente = x.Vigente,
        }));
    }

    string? Validar(EmpleoInput input)
    {
        if (!Egresados.Tipos.Contains(input.Tipo)) return "Elige el tipo.";
        if (input.Inicio.Date > DateTime.Today) return "La fecha de inicio no puede ser futura.";
        if (input.Fin is { } fin && fin.Date < input.Inicio.Date) return "La fecha de fin no puede ser anterior al inicio.";
        return null;
    }

    // G3: la empresa se reutiliza si ya existe con el mismo nombre.
    async Task<int> Empresa(string nombre)
    {
        nombre = nombre.Trim();
        var id = await db.Empresa.Where(x => x.Nombre == nombre).Select(x => (int?)x.IdEmpresa).FirstOrDefaultAsync();
        if (id is not null) return id.Value;
        var nueva = new Empresa { Nombre = nombre };
        db.Empresa.Add(nueva);
        await db.SaveChangesAsync();
        return nueva.IdEmpresa;
    }

    [HttpPost("empleos")]
    public async Task<IActionResult> Agregar(EmpleoInput input)
    {
        if (Validar(input) is { } error) return BadRequest(Error(error));
        var e = await Yo();
        var empleo = new Empleo { ClaveAlumno = e.ClaveAlumno, IdEmpresa = await Empresa(input.Empresa), Tipo = input.Tipo, AreaProfesional = Limpio(input.Area) };
        empleo.DetalleEmpleo.Add(Detalle(new DetalleEmpleo(), input));
        db.Empleo.Add(empleo);
        Renovar(e);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("empleos/{id:int}")]
    public async Task<IActionResult> Editar(int id, EmpleoInput input)
    {
        if (Validar(input) is { } error) return BadRequest(Error(error));
        var e = await Yo();
        var empleo = await db.Empleo.Include(x => x.DetalleEmpleo).SingleOrDefaultAsync(x => x.IdEmpleo == id && x.ClaveAlumno == e.ClaveAlumno);
        if (empleo is null) return NotFound();
        empleo.IdEmpresa = await Empresa(input.Empresa);
        empleo.Tipo = input.Tipo;
        empleo.AreaProfesional = Limpio(input.Area);
        var detalle = empleo.DetalleEmpleo.FirstOrDefault();
        if (detalle is null) empleo.DetalleEmpleo.Add(detalle = new DetalleEmpleo());
        Detalle(detalle, input);
        Renovar(e);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("empleos/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var e = await Yo();
        var empleo = await db.Empleo.Include(x => x.DetalleEmpleo).SingleOrDefaultAsync(x => x.IdEmpleo == id && x.ClaveAlumno == e.ClaveAlumno);
        if (empleo is null) return NotFound();
        db.DetalleEmpleo.RemoveRange(empleo.DetalleEmpleo);
        db.Empleo.Remove(empleo);
        await db.SaveChangesAsync();
        return NoContent();
    }

    static DetalleEmpleo Detalle(DetalleEmpleo d, EmpleoInput input)
    {
        d.Puesto = input.Puesto.Trim();
        d.FechaInicio = input.Inicio.Date;
        d.FechaFinal = input.Fin?.Date;
        d.RelacionPosgrado = input.Relacion;
        d.Sueldo = input.Sueldo;
        return d;
    }

    // G4: «Comentarios» = retroalimentación (RF-02.8).
    [HttpGet("comentarios")]
    public async Task<IActionResult> Comentarios()
    {
        var e = await Yo();
        return Ok(await db.RetroalimentacionEgresado.AsNoTracking().Where(r => r.ClaveAlumno == e.ClaveAlumno)
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.IdRetroalimentacion)
            .Select(r => new { fecha = r.Fecha, comentario = r.Comentario, sugerencia = r.Sugerencia }).ToListAsync());
    }

    [HttpPost("comentarios")]
    public async Task<IActionResult> Comentar(ComentarioInput input)
    {
        var comentario = Limpio(input.Comentario);
        var sugerencia = Limpio(input.Sugerencia);
        if (comentario is null && sugerencia is null) return BadRequest(Error("Escribe un comentario o una sugerencia."));
        var e = await Yo();
        db.RetroalimentacionEgresado.Add(new RetroalimentacionEgresado { ClaveAlumno = e.ClaveAlumno, Fecha = DateTime.Today, Comentario = comentario, Sugerencia = sugerencia });
        await db.SaveChangesAsync();
        return NoContent();
    }

    // G5
    [HttpGet("constancias")]
    public async Task<IActionResult> Constancias()
    {
        var e = await Yo();
        return Ok(await db.Documento.AsNoTracking().Where(d => d.ClaveAlumno == e.ClaveAlumno && d.IdTipoDocumento == TipoDocumento.Constancia)
            .OrderByDescending(d => d.FechaCarga).Select(d => new { id = d.IdDocumento, nombre = d.NombreArchivo, fecha = d.FechaCarga, revisada = d.Verificado }).ToListAsync());
    }

    [HttpPost("constancias"), RequestSizeLimit(DocumentosController.LimitePeticion)]
    public async Task<IActionResult> SubirConstancia(IFormFile? archivo)
    {
        if (await DocumentosController.ValidarPdf(archivo, "Selecciona un archivo PDF.") is { } invalido) return BadRequest(Error(invalido));
        var e = await Yo();
        var (enDisco, ruta, nombre) = await DocumentosController.GuardarPdf(archivo!, config, env);
        db.Documento.Add(new Documento
        {
            ClaveAlumno = e.ClaveAlumno, IdTipoDocumento = TipoDocumento.Constancia, NombreArchivo = nombre, RutaArchivo = enDisco,
            FechaCarga = DateTime.UtcNow, IdUsuario = User.IdUsuario(), Verificado = false,
        });
        try { await db.SaveChangesAsync(); }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }
        return NoContent();
    }
}
