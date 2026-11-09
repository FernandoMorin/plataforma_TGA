using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

[ApiController, Route("api/alumnos"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public class AlumnosController(TgaDbContext db) : ControllerBase
{
    // Consulta de alumnos (RF-01, Figma A1-A2). Devuelve a todos; filtros, pestañas y paginación van en el frontend.
    // shortcut: sin paginar en el servidor, porque la maestría tiene decenas de alumnos; paginar aquí si llegan a miles.
    [HttpGet]
    public async Task<IActionResult> Consulta()
    {
        var filas = await db.Alumno.AsNoTracking().Select(a => new
        {
            a.ClaveAlumno, a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno, a.CorreoInstitucional,
            Generacion = a.IdGeneracionNavigation!.NombreGeneracion,
            EstadoAcademico = a.IdEstadoAcademicoNavigation!.Nombre,
            Modalidad = a.IdModalidadNavigation!.Nombre,
            // El semestre actual es el de la inscripción más reciente.
            Semestre = a.AlumnoDetalle.OrderByDescending(d => d.IdPeriodoNavigation.FechaInicio).Select(d => d.Semestre).FirstOrDefault(),
            Toefl = a.RequisitoToefl.OrderByDescending(t => t.IdToefl).Select(t => t.IdEstadoToeflNavigation!.Nombre).FirstOrDefault(),
            // El trabajo aceptado; si no hay, la solicitud pendiente. Una rechazada no cuenta: el alumno vuelve a empezar.
            Trabajo = a.Tesis.Where(t => t.EstadoSolicitud != "Rechazada")
                .OrderByDescending(t => t.EstadoSolicitud == "Aceptada").ThenByDescending(t => t.IdTesis)
                .Select(t => new { t.EstadoSolicitud, Estado = t.IdEstadoTesisNavigation!.Nombre, t.PorcentajeActual })
                .FirstOrDefault(),
        }).ToListAsync();

        return Ok(filas.Select(a =>
        {
            var titulacion = a.Trabajo is null ? "Sin trabajo registrado"
                : a.Trabajo.EstadoSolicitud == "Pendiente" ? "Solicitud pendiente"
                : a.Trabajo.Estado ?? "Sin iniciar";
            var avance = a.Trabajo?.EstadoSolicitud == "Aceptada" ? a.Trabajo.PorcentajeActual ?? 0 : (decimal?)null;
            return new
            {
                clave = a.ClaveAlumno,
                nombre = string.Join(' ', new[] { a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno }.Where(s => !string.IsNullOrWhiteSpace(s))),
                correo = a.CorreoInstitucional,
                generacion = a.Generacion,
                semestre = a.Semestre,
                estadoAcademico = a.EstadoAcademico,
                modalidad = a.Modalidad ?? "Sin asignar",
                // «Pendiente» = no lo ha presentado, también cuando aún no hay registro.
                toefl = a.Toefl ?? "Pendiente",
                titulacion,
                avance,
                rezago = RezagoPorAvance(a.EstadoAcademico, a.Semestre, avance),
            };
        }));
    }

    // RF-01.9: avance esperado = (semestre - 1) / 3 x 100, con tope de 100; en 1.er semestre no se evalúa.
    // Solo aplica a alumnos activos: una baja o un titulado no están rezagados.
    public static string RezagoPorAvance(string? estadoAcademico, int? semestre, decimal? avance)
    {
        if (estadoAcademico != "Activo" || semestre is null or <= 1) return "No aplica";
        var esperado = Math.Min((semestre.Value - 1) * 100m / 3, 100);
        return (avance ?? 0) < esperado ? "En rezago" : "Al corriente";
    }
}
