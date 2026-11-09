using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

public record DatosAlumnoInput(
    [Required, EmailAddress, MaxLength(100)] string CorreoInstitucional,
    [EmailAddress, MaxLength(100)] string? CorreoPersonal,
    [MaxLength(20)] string? Telefono,
    int IdGeneracion,
    [Range(1, 8)] int Semestre,
    int IdPeriodo,
    int? IdModalidad);
public record CambioEstadoInput(int IdEstado);
public record AprobarToeflInput([Range(0.01, 999.99)] decimal Puntaje);
public record RechazarToeflInput([Required, MaxLength(1000)] string Observaciones);

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

    // RF-01.9: avance esperado = (semestre - 1) / 3 x 100, con tope de 100 (2.º 33 %, 3.º 67 %, 4.º 100 %).
    public static decimal AvanceEsperado(int semestre) => Math.Min((semestre - 1) * 100m / 3, 100);

    // En 1.er semestre no se evalúa, y solo aplica a alumnos activos: una baja o un titulado no están rezagados.
    public static string RezagoPorAvance(string? estadoAcademico, int? semestre, decimal? avance) =>
        estadoAcademico != "Activo" || semestre is null or <= 1 ? "No aplica"
        : (avance ?? 0) < AvanceEsperado(semestre.Value) ? "En rezago" : "Al corriente";

    // El trabajo de titulación vigente: el aceptado; si no hay, la solicitud pendiente (una rechazada no cuenta).
    IQueryable<Models.Tesis> TrabajoVigente(int clave) =>
        db.Tesis.AsNoTracking().Where(t => t.ClaveAlumno == clave && t.EstadoSolicitud != "Rechazada")
            .OrderByDescending(t => t.EstadoSolicitud == "Aceptada").ThenByDescending(t => t.IdTesis);

    // Informes del trabajo en orden cronológico; el número de informe es su posición.
    async Task<List<(int Numero, Models.Revision R, string? Periodo)>> Informes(int idTesis) =>
        (await db.Revision.AsNoTracking().Where(r => r.IdTesis == idTesis)
            .OrderBy(r => r.Fecha == null).ThenBy(r => r.Fecha).ThenBy(r => r.IdRevision)
            .Select(r => new { R = r, Periodo = r.IdPeriodoNavigation!.Nombre }).ToListAsync())
        .Select((x, i) => (i + 1, x.R, x.Periodo)).ToList();

    // Ficha, pestaña Titulación (Figma F2): trabajo, comité tutorial y avance por informe.
    [HttpGet("{clave:int}/titulacion")]
    public async Task<IActionResult> Titulacion(int clave)
    {
        var t = await TrabajoVigente(clave).Select(t => new
        {
            t.IdTesis, t.Titulo, t.EstadoSolicitud, t.PorcentajeActual, t.FechaRegistro, t.FechaSolicitud,
            Estado = t.IdEstadoTesisNavigation!.Nombre, Modalidad = t.IdModalidadNavigation!.Nombre,
        }).FirstOrDefaultAsync();
        if (t is null) return Ok(new { trabajo = (object?)null, comite = (object?)null, informes = Array.Empty<object>() });

        var comite = await db.Comite.AsNoTracking().Where(c => c.IdTesis == t.IdTesis).Select(c => new { c.IdComite, c.Clave }).FirstOrDefaultAsync();
        var integrantes = comite is null ? [] : await db.ComiteProfesor.AsNoTracking()
            .Where(p => p.IdComite == comite.IdComite && p.Activo == true).OrderBy(p => p.IdRolComite)
            .Select(p => new { p.RpeNavigation.Nombre, p.RpeNavigation.ApellidoPaterno, p.RpeNavigation.ApellidoMaterno, Rol = p.IdRolComiteNavigation.Nombre })
            .ToListAsync();
        var aceptado = t.EstadoSolicitud == "Aceptada";

        return Ok(new
        {
            trabajo = new
            {
                titulo = t.Titulo,
                estado = aceptado ? t.Estado ?? "Sin iniciar" : "Solicitud pendiente",
                modalidad = t.Modalidad,
                fechaRegistro = t.FechaRegistro ?? t.FechaSolicitud,
                avance = aceptado ? t.PorcentajeActual ?? 0 : (decimal?)null,
            },
            comite = comite is null ? null : new
            {
                clave = comite.Clave,
                integrantes = integrantes.Select(p => new
                {
                    nombre = string.Join(' ', new[] { p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    rol = p.Rol,
                }),
            },
            informes = (await Informes(t.IdTesis)).Select(x => new
            {
                numero = x.Numero, fecha = x.R.Fecha, avance = x.R.PorcentajeAvance, cerrado = x.R.Estado == "Cerrada",
            }),
        });
    }

    // Ficha, pestaña Evaluaciones (Figma F3): un renglón por informe, del más reciente al más antiguo.
    // Fecha, avance y promedio solo se muestran cuando la evaluación está cerrada.
    [HttpGet("{clave:int}/evaluaciones")]
    public async Task<IActionResult> Evaluaciones(int clave)
    {
        var idTesis = await TrabajoVigente(clave).Where(t => t.EstadoSolicitud == "Aceptada").Select(t => (int?)t.IdTesis).FirstOrDefaultAsync();
        if (idTesis is null) return Ok(Array.Empty<object>());

        var informes = await Informes(idTesis.Value);
        var ids = informes.Select(x => x.R.IdRevision).ToList();
        var actas = await db.Documento.AsNoTracking()
            .Where(d => d.IdTipoDocumento == TipoDocumento.ActaEvaluacion && d.IdRevision != null && ids.Contains(d.IdRevision.Value))
            .Select(d => new { d.IdRevision, d.IdDocumento, d.NombreArchivo }).ToListAsync();

        return Ok(informes.OrderByDescending(x => x.Numero).Select(x =>
        {
            var cerrada = x.R.Estado == "Cerrada";
            var califs = new[] { x.R.CalifDocumento, x.R.CalifExposicion, x.R.CalifDominio, x.R.CalifGradoAvance }.Where(c => c.HasValue).Select(c => (decimal)c!.Value).ToList();
            var acta = actas.FirstOrDefault(a => a.IdRevision == x.R.IdRevision);
            return new
            {
                numero = x.Numero,
                periodo = x.Periodo,
                fecha = cerrada ? x.R.Fecha : null,
                avance = cerrada ? x.R.PorcentajeAvance : null,
                promedio = cerrada && califs.Count > 0 ? Math.Round(califs.Average(), 1) : (decimal?)null,
                estado = x.R.Estado,
                acta = acta is null ? null : new { id = acta.IdDocumento, nombre = acta.NombreArchivo },
            };
        }));
    }

    // Ficha, pestaña Documentos (Figma F4).
    [HttpGet("{clave:int}/documentos")]
    public async Task<IActionResult> Documentos(int clave)
    {
        var docs = await db.Documento.AsNoTracking().Where(d => d.ClaveAlumno == clave)
            .OrderByDescending(d => d.FechaCarga).ThenByDescending(d => d.IdDocumento)
            .Select(d => new
            {
                d.IdDocumento, d.NombreArchivo, d.FechaCarga, d.IdTipoDocumento, d.IdTesis,
                Tipo = d.IdTipoDocumentoNavigation!.Nombre,
                FechaRevision = d.IdRevisionNavigation!.Fecha,
                ClaveComite = d.CveActaNavigation!.IdComiteNavigation.Clave,
            }).ToListAsync();

        return Ok(docs.Select(d => new
        {
            id = d.IdDocumento,
            nombre = d.NombreArchivo,
            tipo = d.Tipo,
            // A qué está asociado, de lo más específico a lo más general.
            asociado = d.FechaRevision is DateTime f ? $"Revisión del {f:dd/MM/yyyy}"
                : d.ClaveComite is not null ? $"Comité {d.ClaveComite}"
                : d.IdTipoDocumento == TipoDocumento.ComprobanteToefl ? "Requisito TOEFL"
                : d.IdTesis is not null ? "Trabajo de titulación"
                : null,
            fechaCarga = d.FechaCarga,
        }));
    }

    // Ficha del alumno, pestaña Resumen (Figma F1).
    [HttpGet("{clave:int}")]
    public async Task<IActionResult> Ficha(int clave)
    {
        var a = await db.Alumno.AsNoTracking().Where(x => x.ClaveAlumno == clave).Select(x => new
        {
            x.ClaveAlumno, x.Nombre, x.ApellidoPaterno, x.ApellidoMaterno,
            x.CorreoInstitucional, x.CorreoPersonal, x.Telefono, x.FechaIngreso,
            Generacion = x.IdGeneracionNavigation!.NombreGeneracion,
            EstadoAcademico = x.IdEstadoAcademicoNavigation!.Nombre,
            Modalidad = x.IdModalidadNavigation!.Nombre,
            x.IdGeneracion, x.IdModalidad, x.IdEstadoAcademico,
            Inscripcion = x.AlumnoDetalle.OrderByDescending(d => d.IdPeriodoNavigation.FechaInicio)
                .Select(d => new { d.Semestre, d.IdPeriodo, Periodo = d.IdPeriodoNavigation.Nombre }).FirstOrDefault(),
            TrabajoAceptado = x.Tesis.Any(t => t.EstadoSolicitud == "Aceptada" && t.IdEstadoTesis == EstadoTesis.Aceptada),
            Toefl = x.RequisitoToefl.OrderByDescending(t => t.IdToefl)
                .Select(t => new { Estado = t.IdEstadoToeflNavigation!.Nombre, t.FechaPresentacion, t.Puntaje, t.Observaciones }).FirstOrDefault(),
            Comprobante = x.Documento.Where(d => d.IdTipoDocumento == TipoDocumento.ComprobanteToefl)
                .OrderByDescending(d => d.FechaCarga).Select(d => new { d.IdDocumento, d.NombreArchivo }).FirstOrDefault(),
            Trabajo = x.Tesis.Where(t => t.EstadoSolicitud != "Rechazada")
                .OrderByDescending(t => t.EstadoSolicitud == "Aceptada").ThenByDescending(t => t.IdTesis)
                .Select(t => new
                {
                    t.EstadoSolicitud, Estado = t.IdEstadoTesisNavigation!.Nombre, t.PorcentajeActual,
                    Evaluaciones = t.Revision.Count,
                    UltimoInforme = t.Revision.Where(r => r.Estado == "Cerrada").Max(r => r.Fecha),
                })
                .FirstOrDefault(),
            Documentos = x.Documento.Count,
        }).SingleOrDefaultAsync();
        if (a is null) return NotFound();

        var aceptado = a.Trabajo?.EstadoSolicitud == "Aceptada";
        var avance = aceptado ? a.Trabajo!.PorcentajeActual ?? 0 : (decimal?)null;
        var semestre = a.Inscripcion?.Semestre;
        return Ok(new
        {
            clave = a.ClaveAlumno,
            a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno,
            correoInstitucional = a.CorreoInstitucional, correoPersonal = a.CorreoPersonal, telefono = a.Telefono,
            fechaIngreso = a.FechaIngreso,
            generacion = a.Generacion,
            semestre,
            periodo = a.Inscripcion?.Periodo,
            estadoAcademico = a.EstadoAcademico,
            modalidad = a.Modalidad ?? "Sin asignar",
            toefl = new
            {
                estado = a.Toefl?.Estado ?? "Pendiente",
                fechaPresentacion = a.Toefl?.FechaPresentacion,
                puntaje = a.Toefl?.Puntaje,
                observaciones = a.Toefl?.Observaciones,
                comprobante = a.Comprobante is null ? null : new { id = a.Comprobante.IdDocumento, nombre = a.Comprobante.NombreArchivo },
            },
            titulacion = new
            {
                estado = a.Trabajo is null ? "Sin trabajo registrado" : aceptado ? a.Trabajo.Estado ?? "Sin iniciar" : "Solicitud pendiente",
                avance,
                esperado = semestre is int s ? AvanceEsperado(s) : (decimal?)null,
                rezago = RezagoPorAvance(a.EstadoAcademico, semestre, avance),
                ultimoInforme = a.Trabajo?.UltimoInforme,
            },
            evaluaciones = a.Trabajo?.Evaluaciones ?? 0,
            documentos = a.Documentos,
            // Para precargar «Editar datos».
            idGeneracion = a.IdGeneracion, idPeriodo = a.Inscripcion?.IdPeriodo, idModalidad = a.IdModalidad,
            // Para «Cambiar estado»: los estados a los que puede pasar y si están disponibles.
            transiciones = (EstadoAcademico.Transiciones.GetValueOrDefault(a.IdEstadoAcademico ?? 0) ?? [])
                .Select(e => new { id = e, disponible = e != EstadoAcademico.Titulado || a.TrabajoAceptado }),
        });
    }

    // Opciones de los selectores de «Editar datos».
    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones() => Ok(new
    {
        generaciones = await db.Generacion.AsNoTracking().OrderByDescending(g => g.AnioIngreso)
            .Select(g => new { id = g.IdGeneracion, nombre = g.NombreGeneracion }).ToListAsync(),
        periodos = await db.PeriodoAcademico.AsNoTracking().OrderByDescending(p => p.FechaInicio)
            .Select(p => new { id = p.IdPeriodo, nombre = p.Nombre }).ToListAsync(),
        modalidades = await db.CatModalidadTitulacion.AsNoTracking().Where(m => m.Activo == true).OrderBy(m => m.IdModalidad)
            .Select(m => new { id = m.IdModalidad, nombre = m.Nombre }).ToListAsync(),
        estados = await db.CatEstadoAcademico.AsNoTracking().OrderBy(e => e.IdEstadoAcademico)
            .Select(e => new { id = e.IdEstadoAcademico, nombre = e.Nombre }).ToListAsync(),
    });

    static object Error(string mensaje) => new { mensaje };

    // Editar datos (Figma F5). El semestre y el periodo forman la inscripción: se crea o actualiza la de ese periodo.
    [HttpPut("{clave:int}/datos")]
    public async Task<IActionResult> EditarDatos(int clave, DatosAlumnoInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var a = await db.Alumno.Include(x => x.IdUsuarioNavigation).SingleOrDefaultAsync(x => x.ClaveAlumno == clave);
        if (a is null) return NotFound();
        if (!await db.Generacion.AnyAsync(g => g.IdGeneracion == input.IdGeneracion)) return BadRequest(Error("La generación no existe."));
        if (!await db.PeriodoAcademico.AnyAsync(p => p.IdPeriodo == input.IdPeriodo)) return BadRequest(Error("El periodo no existe."));
        if (input.IdModalidad is int m && !await db.CatModalidadTitulacion.AnyAsync(x => x.IdModalidad == m)) return BadRequest(Error("La modalidad no existe."));

        var correo = input.CorreoInstitucional.Trim().ToLowerInvariant();
        // El correo institucional es el usuario de inicio de sesión del alumno: se cambia en los dos lados.
        var u = a.IdUsuarioNavigation;
        if (u is not null && u.Correo != correo)
        {
            if (await db.Usuario.AnyAsync(x => x.Correo == correo && x.IdUsuario != u.IdUsuario)) return Conflict(Error("Ese correo ya lo usa otra cuenta."));
            u.Correo = correo;
        }
        a.CorreoInstitucional = correo;
        a.CorreoPersonal = string.IsNullOrWhiteSpace(input.CorreoPersonal) ? null : input.CorreoPersonal.Trim().ToLowerInvariant();
        a.Telefono = string.IsNullOrWhiteSpace(input.Telefono) ? null : input.Telefono.Trim();
        a.IdGeneracion = input.IdGeneracion;
        a.IdModalidad = input.IdModalidad;

        var inscripcion = await db.AlumnoDetalle.SingleOrDefaultAsync(d => d.ClaveAlumno == clave && d.IdPeriodo == input.IdPeriodo);
        if (inscripcion is null)
            db.AlumnoDetalle.Add(new Models.AlumnoDetalle { ClaveAlumno = clave, IdPeriodo = input.IdPeriodo, Semestre = input.Semestre, FechaRegistro = DateTime.Today });
        else
            inscripcion.Semestre = input.Semestre;

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // Cambiar estado académico (Figma F6), según el diagrama de estados.
    // Titulado solo con el trabajo aceptado; la baja definitiva cancela el trabajo en curso (decisión D3).
    // El alta del egresado no es automática: la hace el coordinador en Egresados.
    [HttpPost("{clave:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int clave, CambioEstadoInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var a = await db.Alumno.SingleOrDefaultAsync(x => x.ClaveAlumno == clave);
        if (a is null) return NotFound();
        if (!(EstadoAcademico.Transiciones.GetValueOrDefault(a.IdEstadoAcademico ?? 0) ?? []).Contains(input.IdEstado))
            return BadRequest(Error("No se puede pasar a ese estado desde el estado actual."));

        var trabajo = await db.Tesis.Where(t => t.ClaveAlumno == clave && t.EstadoSolicitud == "Aceptada")
            .OrderByDescending(t => t.IdTesis).FirstOrDefaultAsync();
        if (input.IdEstado == EstadoAcademico.Titulado && trabajo?.IdEstadoTesis != EstadoTesis.Aceptada)
            return BadRequest(Error("Para marcarlo como titulado, su trabajo de titulación debe estar aceptado."));
        if (input.IdEstado == EstadoAcademico.BajaDefinitiva && trabajo is not null && trabajo.IdEstadoTesis != EstadoTesis.Aceptada)
        {
            trabajo.IdEstadoTesis = EstadoTesis.Cancelada;
            trabajo.UltimaActualizacion = DateTime.UtcNow;
        }

        a.IdEstadoAcademico = input.IdEstado;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // Aprobar TOEFL (Figma F7): el coordinador captura el puntaje que viene en el comprobante.
    [HttpPost("{clave:int}/toefl/aprobar")]
    public Task<IActionResult> AprobarToefl(int clave, AprobarToeflInput input) =>
        ResolverToefl(clave, t => { t.IdEstadoToefl = EstadoToefl.Aprobado; t.Puntaje = input.Puntaje; t.Observaciones = null; });

    // No aprobado: regresa a Pendiente con las observaciones del coordinador.
    [HttpPost("{clave:int}/toefl/rechazar")]
    public Task<IActionResult> RechazarToefl(int clave, RechazarToeflInput input) =>
        ResolverToefl(clave, t => { t.IdEstadoToefl = EstadoToefl.Pendiente; t.Puntaje = null; t.Observaciones = input.Observaciones.Trim(); });

    async Task<IActionResult> ResolverToefl(int clave, Action<Models.RequisitoToefl> resolver)
    {
        var t = await db.RequisitoToefl.Where(x => x.ClaveAlumno == clave).OrderByDescending(x => x.IdToefl).FirstOrDefaultAsync();
        if (t is null) return NotFound();
        if (t.IdEstadoToefl != EstadoToefl.Presentado) return BadRequest(Error("Solo se puede resolver un TOEFL presentado."));
        resolver(t);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
