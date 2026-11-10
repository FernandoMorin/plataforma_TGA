using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record RechazoSolicitudInput([Required, MaxLength(255)] string Motivo);
public record ComiteInput(string? Codirector, [Required, MaxLength(10)] string Sinodal1, [Required, MaxLength(10)] string Sinodal2);

// Titulación vista por el profesor (Figma PT1-PT3, PT8). El coordinador también la usa «como profesor».
[ApiController, Route("api/profesor"), Authorize(Roles = $"{Roles.Profesor},{Roles.Coordinador}")]
public class ProfesorController(TgaDbContext db) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    async Task<string?> MiRpe()
    {
        var id = User.IdUsuario();
        return await db.ProfesorDetalle.Where(d => d.IdUsuario == id).Select(d => d.Rpe).FirstOrDefaultAsync();
    }

    // PT1: solicitudes pendientes donde me eligieron como director.
    [HttpGet("solicitudes")]
    public async Task<IActionResult> Solicitudes()
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var lista = await db.Tesis.AsNoTracking()
            .Where(t => t.RpeDirector == rpe && t.EstadoSolicitud == "Pendiente")
            .OrderBy(t => t.FechaSolicitud).ThenBy(t => t.IdTesis)
            .Select(t => new
            {
                t.IdTesis, t.ClaveAlumno, t.Titulo, t.FechaSolicitud,
                Modalidad = t.IdModalidadNavigation!.Nombre,
                Linea = t.IdLineaInvestigacionNavigation!.Nombre,
                Generacion = t.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
                t.ClaveAlumnoNavigation.Nombre, t.ClaveAlumnoNavigation.ApellidoPaterno, t.ClaveAlumnoNavigation.ApellidoMaterno,
            }).ToListAsync();
        return Ok(lista.Select(t => new
        {
            id = t.IdTesis, clave = t.ClaveAlumno, alumno = Titulacion.NombreCompleto(t.Nombre, t.ApellidoPaterno, t.ApellidoMaterno),
            generacion = t.Generacion, fecha = t.FechaSolicitud, modalidad = t.Modalidad, titulo = t.Titulo, linea = t.Linea,
        }));
    }

    async Task<Tesis?> SolicitudPendiente(int id, string rpe) =>
        await db.Tesis.SingleOrDefaultAsync(t => t.IdTesis == id && t.RpeDirector == rpe && t.EstadoSolicitud == "Pendiente");

    [HttpPost("solicitudes/{id:int}/aceptar")]
    public async Task<IActionResult> Aceptar(int id)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var t = await SolicitudPendiente(id, rpe);
        if (t is null) return NotFound(Error("La solicitud ya no está pendiente."));

        t.EstadoSolicitud = "Aceptada";
        t.FechaResolucion = t.FechaRegistro = DateTime.Today;
        t.IdEstadoTesis = EstadoTesis.SinIniciar;
        t.PorcentajeActual = 0;
        t.UltimaActualizacion = DateTime.UtcNow;
        // La modalidad queda fijada al aceptarse la solicitud.
        var alumno = await db.Alumno.SingleAsync(a => a.ClaveAlumno == t.ClaveAlumno);
        alumno.IdModalidad = t.IdModalidad;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // PT8
    [HttpPost("solicitudes/{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, RechazoSolicitudInput input)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var motivo = input.Motivo.Trim();
        if (motivo.Length == 0) return BadRequest(Error("Escribe el motivo."));
        var t = await SolicitudPendiente(id, rpe);
        if (t is null) return NotFound(Error("La solicitud ya no está pendiente."));

        t.EstadoSolicitud = "Rechazada";
        t.MotivoRechazo = motivo;
        t.FechaResolucion = DateTime.Today;
        t.UltimaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // PT2: trabajos aceptados que dirijo o en cuyo comité participo, con lo que me toca hacer en cada uno.
    [HttpGet("alumnos")]
    public async Task<IActionResult> MisAlumnos()
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        await Titulacion.AbrirInformes(db);

        var tesis = await db.Tesis.AsNoTracking()
            .Where(t => t.EstadoSolicitud == "Aceptada" && t.IdEstadoTesis != EstadoTesis.Cancelada
                && (t.RpeDirector == rpe || t.Comite.Any(c => c.ComiteProfesor.Any(m => m.Rpe == rpe && m.Activo == true))))
            .OrderBy(t => t.ClaveAlumnoNavigation.Nombre).ThenBy(t => t.ClaveAlumnoNavigation.ApellidoPaterno)
            .Select(t => new
            {
                t.IdTesis, t.ClaveAlumno, t.RpeDirector, t.PorcentajeActual, t.IdEstadoTesis,
                Modalidad = t.IdModalidadNavigation!.Nombre,
                Generacion = t.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
                t.ClaveAlumnoNavigation.Nombre, t.ClaveAlumnoNavigation.ApellidoPaterno, t.ClaveAlumnoNavigation.ApellidoMaterno,
                TieneComite = t.Comite.Any(),
                MiRol = t.Comite.SelectMany(c => c.ComiteProfesor).Where(m => m.Rpe == rpe && m.Activo == true)
                    .Select(m => m.IdRolComiteNavigation.Nombre).FirstOrDefault(),
                // El informe en curso, con mi revisión.
                Informe = t.Revision.Where(r => r.Estado != "Cerrada").Select(r => new
                {
                    r.IdRevision, r.Estado,
                    Mia = r.RevisionProfesor.Where(p => p.Rpe == rpe).Select(p => new { p.FechaEnvio, p.FechaConformidad }).FirstOrDefault(),
                }).FirstOrDefault(),
            }).ToListAsync();
        var completas = await Titulacion.TesisConComiteCompleto(db, tesis.Select(t => t.IdTesis));

        return Ok(tesis.Select(t =>
        {
            var director = t.RpeDirector == rpe;
            var comite = completas.Contains(t.IdTesis) ? "Completo" : t.TieneComite ? "Incompleto" : "Sin armar";
            var i = t.Informe;
            var accion =
                director && comite != "Completo" ? "Armar comité"
                : i?.Estado == "Abierta" && i.Mia?.FechaEnvio is null ? "Revisar"
                : i?.Estado == "En consolidación" && director ? "Consolidar"
                : i?.Estado == "En firma" && i.Mia?.FechaConformidad is null ? "Firmar"
                : director && i is null && t.PorcentajeActual >= 100 && t.IdEstadoTesis != EstadoTesis.Aceptada ? "Registrar resolución"
                : null;
            return new
            {
                id = t.IdTesis, clave = t.ClaveAlumno, alumno = Titulacion.NombreCompleto(t.Nombre, t.ApellidoPaterno, t.ApellidoMaterno),
                generacion = t.Generacion, modalidad = t.Modalidad, miRol = t.MiRol ?? (director ? "Director" : null),
                comite, avance = t.PorcentajeActual ?? 0, accion, idRevision = i?.IdRevision,
            };
        }));
    }

    async Task<Tesis?> TrabajoQueDirijo(int idTesis, string rpe) =>
        await db.Tesis.SingleOrDefaultAsync(t => t.IdTesis == idTesis && t.RpeDirector == rpe && t.EstadoSolicitud == "Aceptada"
            && t.IdEstadoTesis != EstadoTesis.Aceptada && t.IdEstadoTesis != EstadoTesis.Cancelada);

    // PT3: el comité actual (solo integrantes vigentes) y los profesores que se pueden elegir.
    [HttpGet("comite/{idTesis:int}")]
    public async Task<IActionResult> Comite(int idTesis)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var t = await TrabajoQueDirijo(idTesis, rpe);
        if (t is null) return NotFound();

        var alumno = await db.Alumno.AsNoTracking().Where(a => a.ClaveAlumno == t.ClaveAlumno)
            .Select(a => new { a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno }).SingleAsync();
        var miembros = await Titulacion.MiembrosVigentes(db).AsNoTracking()
            .Where(m => m.IdComiteNavigation.IdTesis == idTesis && m.Rpe != rpe)
            .OrderBy(m => m.IdComiteProfesor).Select(m => new { m.Rpe, m.IdRolComite }).ToListAsync();
        var sinodales = miembros.Where(m => m.IdRolComite == RolComite.Sinodal).Select(m => m.Rpe).ToList();
        var profesores = await Titulacion.Vigentes(db).AsNoTracking().Where(p => p.Rpe != rpe)
            .Select(p => new { p.Rpe, p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno, Externo = p.ProfesorDetalle.Any(d => d.Externo) })
            .ToListAsync();
        var yo = await db.Profesor.AsNoTracking().Where(p => p.Rpe == rpe).Select(p => new { p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno }).SingleAsync();

        return Ok(new
        {
            alumno = Titulacion.NombreCompleto(alumno.Nombre, alumno.ApellidoPaterno, alumno.ApellidoMaterno),
            clave = t.ClaveAlumno,
            director = Titulacion.NombreCompleto(yo.Nombre, yo.ApellidoPaterno, yo.ApellidoMaterno),
            codirector = miembros.FirstOrDefault(m => m.IdRolComite == RolComite.Codirector)?.Rpe,
            sinodal1 = sinodales.ElementAtOrDefault(0),
            sinodal2 = sinodales.ElementAtOrDefault(1),
            profesores = profesores.Select(p => new { rpe = p.Rpe, nombre = Titulacion.NombreCompleto(p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno), externo = p.Externo })
                .OrderBy(p => p.nombre),
        });
    }

    // PT3: registra o completa el comité. Quien cambia deja su fila con fecha de baja y el cambio queda en comite_historial.
    [HttpPut("comite/{idTesis:int}")]
    public async Task<IActionResult> GuardarComite(int idTesis, ComiteInput input)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var codirector = string.IsNullOrWhiteSpace(input.Codirector) ? null : input.Codirector;
        var elegidos = new[] { codirector, input.Sinodal1, input.Sinodal2 }.Where(r => r is not null).ToList();
        if (elegidos.Distinct().Count() != elegidos.Count) return BadRequest(Error("Un profesor no puede ocupar dos lugares."));
        if (elegidos.Contains(rpe)) return BadRequest(Error("Ya estás en el comité como director."));
        var vigentes = await Titulacion.Vigentes(db).Where(p => elegidos.Contains(p.Rpe)).CountAsync();
        if (vigentes != elegidos.Count) return BadRequest(Error("Elige profesores de la lista."));

        await using var tx = await db.Database.BeginTransactionAsync();
        var t = await TrabajoQueDirijo(idTesis, rpe);
        if (t is null) return NotFound();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id_tesis FROM tesis WHERE id_tesis = {idTesis} FOR UPDATE");

        var comite = await db.Comite.Include(c => c.ComiteProfesor).SingleOrDefaultAsync(c => c.IdTesis == idTesis);
        if (comite is null)
        {
            var anio = DateTime.Today.Year;
            var prefijo = $"CT-{anio}-";
            var ultima = await db.Comite.Where(c => c.Clave.StartsWith(prefijo)).Select(c => c.Clave).MaxAsync(c => (string?)c);
            var siguiente = ultima is null ? 1 : int.Parse(ultima[prefijo.Length..]) + 1;
            comite = new Comite { IdTesis = idTesis, Clave = $"{prefijo}{siguiente:D4}" };
            db.Comite.Add(comite);
        }

        var hoy = DateTime.Today;
        var deseado = new List<(string Rpe, int Rol)> { (rpe, RolComite.Director), (input.Sinodal1, RolComite.Sinodal), (input.Sinodal2, RolComite.Sinodal) };
        if (codirector is not null) deseado.Add((codirector, RolComite.Codirector));

        var salen = comite.ComiteProfesor.Where(m => m.Activo == true && !deseado.Contains((m.Rpe, m.IdRolComite))).ToList();
        foreach (var m in salen)
        {
            m.Activo = false;
            m.FechaBaja = hoy;
        }
        var entran = deseado.Where(d => !comite.ComiteProfesor.Any(m => m.Activo == true && m.Rpe == d.Rpe && m.IdRolComite == d.Rol)).ToList();
        foreach (var (r, rol) in entran)
            comite.ComiteProfesor.Add(new ComiteProfesor { Rpe = r, IdRolComite = rol, FechaAsignacion = hoy, Activo = true });
        Titulacion.RegistrarHistorial(db, comite, salen.Select(m => (m.Rpe, m.IdRolComite)).ToList(), entran, hoy, User.IdUsuario());

        t.UltimaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync();
        // Con un informe en curso: se descartan las revisiones de quien salió y el informe se reajusta.
        var quienesSiguen = deseado.Select(d => d.Rpe).ToHashSet();
        await Titulacion.AjustarInformeTrasCambio(db, idTesis, salen.Select(m => m.Rpe).Where(r => !quienesSiguen.Contains(r)).ToList());
        await tx.CommitAsync();
        return NoContent();
    }
}
