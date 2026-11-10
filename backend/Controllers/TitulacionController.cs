using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

// Titulación del coordinador (Figma CT1): todos los trabajos, de la solicitud a la conclusión.
[ApiController, Route("api/titulacion"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public class TitulacionController(TgaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consulta()
    {
        await Titulacion.AbrirInformes(db);
        // Una solicitud rechazada no es un trabajo: el alumno vuelve a empezar.
        var trabajos = await db.Tesis.AsNoTracking().Where(t => t.EstadoSolicitud != "Rechazada")
            .OrderBy(t => t.ClaveAlumnoNavigation.Nombre).ThenBy(t => t.ClaveAlumnoNavigation.ApellidoPaterno)
            .Select(t => new
            {
                t.IdTesis, t.ClaveAlumno, t.Titulo, t.EstadoSolicitud, t.IdEstadoTesis, t.PorcentajeActual,
                Modalidad = t.IdModalidadNavigation!.Nombre,
                Generacion = t.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
                t.ClaveAlumnoNavigation.Nombre, t.ClaveAlumnoNavigation.ApellidoPaterno, t.ClaveAlumnoNavigation.ApellidoMaterno,
                Director = new { t.RpeDirectorNavigation!.Nombre, t.RpeDirectorNavigation.ApellidoPaterno, t.RpeDirectorNavigation.ApellidoMaterno },
                TieneComite = t.Comite.Any(),
                Informes = t.Revision.Count,
                EnCurso = t.Revision.Where(r => r.Estado != "Cerrada").Select(r => r.Estado).FirstOrDefault(),
            }).ToListAsync();
        var completas = await Titulacion.TesisConComiteCompleto(db, trabajos.Select(t => t.IdTesis));

        return Ok(trabajos.Select(t =>
        {
            var aceptado = t.EstadoSolicitud == "Aceptada";
            var comite = !aceptado ? null : completas.Contains(t.IdTesis) ? "Completo" : t.TieneComite ? "Incompleto" : "Sin armar";
            // Pestañas de CT1: Solicitudes, Sin comité, En evaluación y Concluidos.
            var grupo = !aceptado ? "Solicitudes"
                : t.IdEstadoTesis == EstadoTesis.Aceptada ? "Concluidos"
                : t.IdEstadoTesis == EstadoTesis.Cancelada ? "Cancelados"
                : comite != "Completo" ? "Sin comité"
                : t.EnCurso is not null ? "En evaluación" : "En progreso";
            var informe = !aceptado ? "Solicitud pendiente"
                : t.IdEstadoTesis == EstadoTesis.Aceptada ? "Concluido"
                : t.IdEstadoTesis == EstadoTesis.Cancelada ? "Cancelado"
                : t.EnCurso is null ? null
                : $"Informe {t.Informes} · {(t.EnCurso == "Abierta" ? "Abierto" : t.EnCurso)}";
            return new
            {
                id = t.IdTesis, clave = t.ClaveAlumno, alumno = Titulacion.NombreCompleto(t.Nombre, t.ApellidoPaterno, t.ApellidoMaterno),
                titulo = t.Titulo, modalidad = t.Modalidad, generacion = t.Generacion,
                director = Titulacion.NombreCompleto(t.Director.Nombre, t.Director.ApellidoPaterno, t.Director.ApellidoMaterno),
                comite, avance = aceptado ? t.PorcentajeActual ?? 0 : (decimal?)null, informe, enCurso = t.EnCurso, grupo,
            };
        }));
    }
}
