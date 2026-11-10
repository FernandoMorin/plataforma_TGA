using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

public record SolicitudInput(int IdModalidad, [Required, MaxLength(255)] string Titulo, int IdLineaInvestigacion, [Required, MaxLength(10)] string RpeDirector);

// Titulación vista por el alumno (Figma AT1-AT7).
[ApiController, Route("api/mi-titulacion"), Authorize(Roles = Roles.Alumno)]
public class MiTitulacionController(TgaDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    static readonly int[] TiposEvidencia = [TipoDocumento.Avance, TipoDocumento.Evidencia, TipoDocumento.TrabajoFinal];

    // El trabajo aceptado del alumno (el que tiene comité, informes y evidencias).
    async Task<Models.Tesis?> TrabajoAceptado(int clave) =>
        await db.Tesis.AsNoTracking().Where(t => t.ClaveAlumno == clave && t.EstadoSolicitud == "Aceptada").OrderByDescending(t => t.IdTesis).FirstOrDefaultAsync();

    static object Error(string mensaje) => new { mensaje };

    Task<int> ClaveAlumno()
    {
        var id = User.IdUsuario();
        return db.Alumno.Where(a => a.IdUsuario == id).Select(a => a.ClaveAlumno).SingleAsync();
    }

    // Mi trabajo: sin solicitud (AT1), solicitud pendiente (AT2) o rechazada (AT7), o el trabajo aceptado (AT3).
    [HttpGet]
    public async Task<IActionResult> MiTrabajo()
    {
        var clave = await ClaveAlumno();
        await Titulacion.AbrirInformes(db);
        var t = await db.Tesis.AsNoTracking().Where(x => x.ClaveAlumno == clave).OrderByDescending(x => x.IdTesis)
            .Select(x => new
            {
                x.EstadoSolicitud, x.Titulo, x.IdModalidad, x.IdLineaInvestigacion, x.RpeDirector,
                Modalidad = x.IdModalidadNavigation!.Nombre,
                Linea = x.IdLineaInvestigacionNavigation!.Nombre,
                Director = new { x.RpeDirectorNavigation!.Nombre, x.RpeDirectorNavigation.ApellidoPaterno, x.RpeDirectorNavigation.ApellidoMaterno },
                x.FechaSolicitud, x.FechaResolucion, x.MotivoRechazo, x.FechaRegistro, x.PorcentajeActual,
                Estado = x.IdEstadoTesisNavigation!.Nombre,
                Informes = x.Revision.OrderBy(r => r.IdRevision)
                    .Select(r => new { r.Estado, r.Fecha, Fin = r.IdPeriodoNavigation!.FechaFin }).ToList(),
            })
            .FirstOrDefaultAsync();
        if (t is null) return Ok(new { solicitud = (object?)null, trabajo = (object?)null });

        var director = t.RpeDirector is null ? null : Titulacion.NombreCompleto(t.Director.Nombre, t.Director.ApellidoPaterno, t.Director.ApellidoMaterno);
        if (t.EstadoSolicitud != "Aceptada")
        {
            return Ok(new
            {
                solicitud = new
                {
                    estado = t.EstadoSolicitud, modalidad = t.Modalidad, titulo = t.Titulo, linea = t.Linea, director,
                    idModalidad = t.IdModalidad, idLineaInvestigacion = t.IdLineaInvestigacion,
                    fechaSolicitud = t.FechaSolicitud, fechaResolucion = t.FechaResolucion, motivoRechazo = t.MotivoRechazo,
                },
                trabajo = (object?)null,
            });
        }

        // Número de informe = orden dentro del trabajo.
        var numerados = t.Informes.Select((r, i) => new { Numero = i + 1, r.Estado, r.Fecha, r.Fin }).ToList();
        var ultimo = numerados.LastOrDefault(r => r.Estado == "Cerrada");
        var siguiente = numerados.FirstOrDefault(r => r.Estado != "Cerrada");
        return Ok(new
        {
            solicitud = (object?)null,
            trabajo = new
            {
                modalidad = t.Modalidad, estado = t.Estado ?? "Sin iniciar", titulo = t.Titulo, director, linea = t.Linea,
                fechaRegistro = t.FechaRegistro, avance = t.PorcentajeActual ?? 0,
                ultimoInforme = ultimo is null ? null : new { numero = ultimo.Numero, fecha = ultimo.Fecha },
                siguiente = siguiente is null ? null : new { numero = siguiente.Numero, fin = siguiente.Fin },
            },
        });
    }

    // Opciones de la solicitud (AT1).
    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones()
    {
        var modalidades = await db.CatModalidadTitulacion.AsNoTracking().Where(m => m.Activo != false)
            .OrderBy(m => m.IdModalidad).Select(m => new { id = m.IdModalidad, nombre = m.Nombre }).ToListAsync();
        var lineas = await db.CatLineaInvestigacion.AsNoTracking().Where(l => l.Activo)
            .OrderBy(l => l.Nombre).Select(l => new { id = l.IdLineaInvestigacion, nombre = l.Nombre }).ToListAsync();
        var profesores = await DirectoresPosibles().AsNoTracking()
            .Select(p => new { p.Rpe, p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno }).ToListAsync();
        var directores = profesores.Select(p => new { rpe = p.Rpe, nombre = Titulacion.NombreCompleto(p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno) })
            .OrderBy(p => p.nombre).ToList();
        return Ok(new { modalidades, lineas, directores });
    }

    // El director solo puede ser profesor de planta.
    IQueryable<Models.Profesor> DirectoresPosibles() =>
        Titulacion.Vigentes(db).Where(p => p.ProfesorDetalle.Any(d => !d.Externo));

    [HttpPost("solicitud")]
    public async Task<IActionResult> Solicitar(SolicitudInput input)
    {
        var clave = await ClaveAlumno();
        var titulo = input.Titulo.Trim();
        if (titulo.Length == 0) return BadRequest(Error("Escribe el título del trabajo."));
        if (!await db.CatModalidadTitulacion.AnyAsync(m => m.IdModalidad == input.IdModalidad && m.Activo != false))
            return BadRequest(Error("Elige una modalidad."));
        if (!await db.CatLineaInvestigacion.AnyAsync(l => l.IdLineaInvestigacion == input.IdLineaInvestigacion && l.Activo))
            return BadRequest(Error("Elige una línea de investigación."));
        if (!await DirectoresPosibles().AnyAsync(p => p.Rpe == input.RpeDirector))
            return BadRequest(Error("Elige un director de la lista."));

        var alumno = await db.Alumno.AsNoTracking().Where(a => a.ClaveAlumno == clave)
            .Select(a => new { a.IdEstadoAcademico, a.IdPrograma }).SingleAsync();
        if (alumno.IdEstadoAcademico != EstadoAcademico.Activo)
            return Conflict(Error("Solo un alumno activo puede enviar la solicitud."));

        await using var tx = await db.Database.BeginTransactionAsync();
        // Bloquea al alumno para que dos envíos simultáneos no creen dos solicitudes.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT clave_alumno FROM alumno WHERE clave_alumno = {clave} FOR UPDATE");
        // Una rechazada no cuenta: el alumno puede volver a pedir.
        if (await db.Tesis.AnyAsync(t => t.ClaveAlumno == clave && t.EstadoSolicitud != "Rechazada"))
            return Conflict(Error("Ya tienes una solicitud enviada."));

        db.Tesis.Add(new Models.Tesis
        {
            ClaveAlumno = clave,
            IdPrograma = alumno.IdPrograma,
            Titulo = titulo,
            IdModalidad = input.IdModalidad,
            IdLineaInvestigacion = input.IdLineaInvestigacion,
            RpeDirector = input.RpeDirector,
            EstadoSolicitud = "Pendiente",
            FechaSolicitud = DateTime.Today,
            UltimaActualizacion = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // AT4: integrantes vigentes del comité.
    [HttpGet("comite")]
    public async Task<IActionResult> Comite()
    {
        var t = await TrabajoAceptado(await ClaveAlumno());
        var comite = t is null ? null : await db.Comite.AsNoTracking().Where(c => c.IdTesis == t.IdTesis)
            .Select(c => new { c.Clave, Registrado = c.ComiteProfesor.Min(m => m.FechaAsignacion) }).SingleOrDefaultAsync();
        if (t is null || comite is null) return Ok(new { clave = (string?)null, registrado = (DateTime?)null, integrantes = Array.Empty<object>() });

        var integrantes = await Titulacion.MiembrosVigentes(db).AsNoTracking().Where(m => m.IdComiteNavigation.IdTesis == t.IdTesis)
            .OrderBy(m => m.IdRolComite).ThenBy(m => m.IdComiteProfesor)
            .Select(m => new
            {
                m.RpeNavigation.Nombre, m.RpeNavigation.ApellidoPaterno, m.RpeNavigation.ApellidoMaterno, Rol = m.IdRolComiteNavigation.Nombre,
                Correo = m.RpeNavigation.ProfesorDetalle.Select(d => d.CorreoInstitucional ?? d.IdUsuarioNavigation!.Correo).FirstOrDefault(),
            }).ToListAsync();
        return Ok(new
        {
            clave = comite.Clave, registrado = comite.Registrado,
            integrantes = integrantes.Select(m => new { nombre = Titulacion.NombreCompleto(m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno), rol = m.Rol, correo = m.Correo }),
        });
    }

    // AT5: informes del más reciente al más antiguo; el detalle solo de los cerrados.
    [HttpGet("evaluaciones")]
    public async Task<IActionResult> Evaluaciones()
    {
        var t = await TrabajoAceptado(await ClaveAlumno());
        if (t is null) return Ok(Array.Empty<object>());
        return Ok(await Titulacion.InformesDelTrabajo(db, t.IdTesis));
    }

    // AT6: a qué informe va lo que suba (el abierto o, si no hay, el siguiente) y lo que ya subió.
    async Task<(int Numero, int? IdRevision, Dictionary<int, int> Numeros)> Destino(int idTesis)
    {
        var informes = await db.Revision.Where(r => r.IdTesis == idTesis).OrderBy(r => r.IdRevision).Select(r => new { r.IdRevision, r.Estado }).ToListAsync();
        var numeros = informes.Select((r, i) => (r.IdRevision, i + 1)).ToDictionary(x => x.IdRevision, x => x.Item2);
        var abierto = informes.FirstOrDefault(r => r.Estado == "Abierta");
        return abierto is null ? (informes.Count + 1, null, numeros) : (numeros[abierto.IdRevision], abierto.IdRevision, numeros);
    }

    [HttpGet("evidencias")]
    public async Task<IActionResult> Evidencias()
    {
        var clave = await ClaveAlumno();
        await Titulacion.AbrirInformes(db);
        var t = await TrabajoAceptado(clave);
        if (t is null) return Ok(new { puedeSubir = false, informe = (int?)null, evidencias = Array.Empty<object>() });
        var (numero, _, numeros) = await Destino(t.IdTesis);
        var docs = await db.Documento.AsNoTracking()
            .Where(d => d.IdTesis == t.IdTesis && d.IdTipoDocumento != null && TiposEvidencia.Contains(d.IdTipoDocumento.Value))
            .OrderByDescending(d => d.FechaCarga)
            .Select(d => new { d.IdDocumento, d.NombreArchivo, Tipo = d.IdTipoDocumentoNavigation!.Nombre, d.IdRevision, d.FechaCarga }).ToListAsync();
        return Ok(new
        {
            puedeSubir = t.IdEstadoTesis != EstadoTesis.Aceptada && t.IdEstadoTesis != EstadoTesis.Cancelada,
            informe = numero,
            evidencias = docs.Select(d => new
            {
                id = d.IdDocumento, nombre = d.NombreArchivo, tipo = d.Tipo,
                informe = d.IdRevision is { } r && numeros.TryGetValue(r, out var n) ? n : numero, fecha = d.FechaCarga,
            }),
        });
    }

    [HttpPost("evidencias"), RequestSizeLimit(DocumentosController.LimitePeticion)]
    public async Task<IActionResult> SubirEvidencia([FromForm] int tipo, IFormFile? archivo)
    {
        if (!TiposEvidencia.Contains(tipo)) return BadRequest(Error("Elige el tipo de documento."));
        if (await DocumentosController.ValidarPdf(archivo, "Selecciona un archivo PDF.") is { } invalido) return BadRequest(Error(invalido));
        var clave = await ClaveAlumno();
        var t = await TrabajoAceptado(clave);
        if (t is null || t.IdEstadoTesis == EstadoTesis.Aceptada || t.IdEstadoTesis == EstadoTesis.Cancelada)
            return Conflict(Error("Tu trabajo no recibe evidencias."));

        // Sin informe abierto, la evidencia se queda sin informe y se asocia al siguiente cuando se abra.
        var (_, idRevision, _) = await Destino(t.IdTesis);
        var (enDisco, ruta, nombre) = await DocumentosController.GuardarPdf(archivo!, config, env);
        db.Documento.Add(new Models.Documento
        {
            ClaveAlumno = clave, IdTesis = t.IdTesis, IdRevision = idRevision, IdTipoDocumento = tipo,
            NombreArchivo = nombre, RutaArchivo = enDisco, FechaCarga = DateTime.UtcNow, IdUsuario = User.IdUsuario(),
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }
        return NoContent();
    }
}
