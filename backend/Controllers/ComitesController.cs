using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record CambioComiteInput(int? IdComiteProfesor, [MaxLength(10)] string? Rpe, int? Rol, DateTime Fecha);

// Comités tutoriales del coordinador (Figma CM1-CM4): consulta, detalle con cambios e historial, y participación docente.
[ApiController, Route("api/comites"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public class ComitesController(TgaDbContext db) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };
    static string Rol(int id) => id switch { RolComite.Director => "Director", RolComite.Codirector => "Codirector", _ => "Sinodal" };

    // Lo que le falta a un comité vigente para estar completo («Falta 1 sinodal»), o null si está completo.
    static string? Faltante(IEnumerable<int> roles)
    {
        var r = roles.ToList();
        var faltas = new List<string>();
        if (!r.Contains(RolComite.Director)) faltas.Add("director");
        var sinodales = 2 - r.Count(x => x == RolComite.Sinodal);
        if (sinodales > 0) faltas.Add(sinodales == 1 ? "1 sinodal" : "2 sinodales");
        return faltas.Count == 0 ? null : "Falta " + string.Join(" y ", faltas);
    }

    // CM1
    [HttpGet]
    public async Task<IActionResult> Consulta()
    {
        var miembros = Titulacion.MiembrosVigentes(db);
        var comites = await db.Comite.AsNoTracking().OrderByDescending(c => c.Clave).Select(c => new
        {
            c.IdComite, c.Clave, c.IdTesisNavigation.ClaveAlumno, c.IdTesisNavigation.IdEstadoTesis,
            Modalidad = c.IdTesisNavigation.IdModalidadNavigation!.Nombre,
            Generacion = c.IdTesisNavigation.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
            c.IdTesisNavigation.ClaveAlumnoNavigation.Nombre, c.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoPaterno,
            c.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoMaterno,
            Desde = c.ComiteProfesor.Min(m => m.FechaAsignacion),
            Integrantes = miembros.Where(m => m.IdComite == c.IdComite).OrderBy(m => m.IdRolComite).ThenBy(m => m.IdComiteProfesor)
                .Select(m => new { m.IdRolComite, m.RpeNavigation.Nombre, m.RpeNavigation.ApellidoPaterno, m.RpeNavigation.ApellidoMaterno }).ToList(),
        }).ToListAsync();

        return Ok(comites.Select(c =>
        {
            var concluido = c.IdEstadoTesis is EstadoTesis.Aceptada or EstadoTesis.Cancelada;
            var faltante = Faltante(c.Integrantes.Select(i => i.IdRolComite));
            var director = c.Integrantes.FirstOrDefault(i => i.IdRolComite == RolComite.Director);
            return new
            {
                id = c.IdComite, clave = c.Clave, claveAlumno = c.ClaveAlumno, modalidad = c.Modalidad, generacion = c.Generacion,
                alumno = Titulacion.NombreCompleto(c.Nombre, c.ApellidoPaterno, c.ApellidoMaterno),
                director = director is null ? null : Titulacion.NombreCompleto(director.Nombre, director.ApellidoPaterno, director.ApellidoMaterno),
                integrantes = c.Integrantes.Select(i => new { nombre = Titulacion.NombreCompleto(i.Nombre, i.ApellidoPaterno, i.ApellidoMaterno), director = i.IdRolComite == RolComite.Director }),
                estado = concluido ? "Concluido" : faltante ?? "Completo",
                grupo = concluido ? "Concluidos" : faltante is null ? "Completos" : "Incompletos",
                desde = c.Desde,
            };
        }));
    }

    // CM2: integrantes vigentes, historial y los profesores que pueden entrar.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var c = await db.Comite.AsNoTracking().Where(x => x.IdComite == id).Select(x => new
        {
            x.Clave, x.IdTesis, x.IdTesisNavigation.Titulo, x.IdTesisNavigation.IdEstadoTesis, x.IdTesisNavigation.ClaveAlumno,
            Modalidad = x.IdTesisNavigation.IdModalidadNavigation!.Nombre,
            x.IdTesisNavigation.ClaveAlumnoNavigation.Nombre, x.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoPaterno,
            x.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoMaterno,
        }).SingleOrDefaultAsync();
        if (c is null) return NotFound();

        var vigentes = await Titulacion.MiembrosVigentes(db).Where(m => m.IdComite == id).Select(m => m.IdComiteProfesor).ToListAsync();
        var filas = await db.ComiteProfesor.AsNoTracking().Where(m => m.IdComite == id).OrderBy(m => m.IdComiteProfesor)
            .Select(m => new { m.IdComiteProfesor, m.Rpe, m.IdRolComite, m.FechaAsignacion, m.FechaBaja, m.Activo, m.RpeNavigation.Nombre, m.RpeNavigation.ApellidoPaterno, m.RpeNavigation.ApellidoMaterno })
            .ToListAsync();
        string NombreDe(string rpe) { var f = filas.First(x => x.Rpe == rpe); return Titulacion.NombreCompleto(f.Nombre, f.ApellidoPaterno, f.ApellidoMaterno); }

        // Historial exacto (RF-03.6), del más reciente al más antiguo. Un reemplazo se muestra en dos renglones (entra y sale).
        var registros = await db.ComiteHistorial.AsNoTracking().Where(h => h.IdComite == id)
            .OrderByDescending(h => h.FechaRegistro).ThenByDescending(h => h.IdHistorial).ToListAsync();
        var rpes = registros.SelectMany(h => new[] { h.RpeSale, h.RpeEntra }).OfType<string>().Distinct().ToList();
        var nombres = (await db.Profesor.AsNoTracking().Where(x => rpes.Contains(x.Rpe)).Select(x => new { x.Rpe, x.Nombre, x.ApellidoPaterno, x.ApellidoMaterno }).ToListAsync())
            .ToDictionary(x => x.Rpe, x => Titulacion.NombreCompleto(x.Nombre, x.ApellidoPaterno, x.ApellidoMaterno));
        string? R(int? rol) => rol is null ? null : Rol(rol.Value);
        var historial = registros.SelectMany(h => h.Accion switch
        {
            "Reemplazo" => new[]
            {
                new { fecha = h.Fecha, accion = h.Accion, integrante = $"{nombres[h.RpeEntra!]} (entra)", rolAnterior = (string?)null, rolNuevo = R(h.RolNuevo) },
                new { fecha = h.Fecha, accion = h.Accion, integrante = $"{nombres[h.RpeSale!]} (sale)", rolAnterior = R(h.RolAnterior), rolNuevo = (string?)null },
            },
            _ => [new { fecha = h.Fecha, accion = h.Accion, integrante = nombres[(h.RpeEntra ?? h.RpeSale)!], rolAnterior = R(h.RolAnterior), rolNuevo = R(h.RolNuevo) }],
        }).ToList();

        var profesores = await Titulacion.Vigentes(db).AsNoTracking()
            .Select(p => new { p.Rpe, p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno, Externo = p.ProfesorDetalle.Any(d => d.Externo) }).ToListAsync();
        var integrantes = filas.Where(f => vigentes.Contains(f.IdComiteProfesor)).OrderBy(f => f.IdRolComite).ThenBy(f => f.IdComiteProfesor).ToList();
        var concluido = c.IdEstadoTesis is EstadoTesis.Aceptada or EstadoTesis.Cancelada;
        var faltante = Faltante(integrantes.Select(i => i.IdRolComite));

        return Ok(new
        {
            clave = c.Clave, claveAlumno = c.ClaveAlumno, alumno = Titulacion.NombreCompleto(c.Nombre, c.ApellidoPaterno, c.ApellidoMaterno),
            modalidad = c.Modalidad, titulo = c.Titulo, estado = concluido ? "Concluido" : faltante ?? "Completo",
            editable = !concluido,
            integrantes = integrantes.Select(i => new { id = i.IdComiteProfesor, rpe = i.Rpe, nombre = NombreDe(i.Rpe), rol = Rol(i.IdRolComite), idRol = i.IdRolComite, desde = i.FechaAsignacion }),
            historial,
            profesores = profesores.Select(p => new { rpe = p.Rpe, nombre = Titulacion.NombreCompleto(p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno), externo = p.Externo })
                .OrderBy(p => p.nombre),
        });
    }

    // CM3 y los demás cambios: reemplazar, agregar, cambiar rol o quitar un integrante. Se permiten con un informe en curso.
    [HttpPost("{id:int}/{cambio:regex(^(reemplazar|agregar|rol|quitar)$)}")]
    public async Task<IActionResult> Cambiar(int id, string cambio, CambioComiteInput input)
    {
        var fecha = input.Fecha.Date;
        if (fecha > DateTime.Today) return BadRequest(Error("La fecha del cambio no puede ser futura."));
        if (input.Rol is { } r && r is not (RolComite.Director or RolComite.Codirector or RolComite.Sinodal)) return BadRequest(Error("Elige un rol."));

        await using var tx = await db.Database.BeginTransactionAsync();
        var comite = await db.Comite.Include(c => c.IdTesisNavigation).SingleOrDefaultAsync(c => c.IdComite == id);
        if (comite is null) return NotFound();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id_tesis FROM tesis WHERE id_tesis = {comite.IdTesis} FOR UPDATE");
        var tesis = comite.IdTesisNavigation;
        if (tesis.IdEstadoTesis is EstadoTesis.Aceptada or EstadoTesis.Cancelada) return Conflict(Error("El comité ya concluyó."));

        var idsVigentes = await Titulacion.MiembrosVigentes(db).Where(m => m.IdComite == id).Select(m => m.IdComiteProfesor).ToListAsync();
        var vigentes = await db.ComiteProfesor.Where(m => idsVigentes.Contains(m.IdComiteProfesor)).ToListAsync();
        var actual = cambio == "agregar" ? null : vigentes.SingleOrDefault(m => m.IdComiteProfesor == input.IdComiteProfesor);
        if (cambio != "agregar" && actual is null) return BadRequest(Error("Elige un integrante del comité."));
        if (actual is not null && fecha < actual.FechaAsignacion) return BadRequest(Error("La fecha no puede ser anterior a la entrada del integrante."));
        if (cambio == "quitar" && actual!.IdRolComite == RolComite.Director) return BadRequest(Error("El director no se puede quitar; reemplázalo."));
        if (cambio == "rol" && (input.Rol is null || input.Rol == actual!.IdRolComite)) return BadRequest(Error("Elige un rol distinto."));

        // Quién entra y con qué rol.
        var entra = cambio switch
        {
            "reemplazar" or "agregar" => (Rpe: input.Rpe, Rol: input.Rol ?? actual?.IdRolComite),
            "rol" => (Rpe: actual!.Rpe, Rol: input.Rol),
            _ => (Rpe: (string?)null, Rol: (int?)null),
        };
        if (cambio is "reemplazar" or "agregar")
        {
            if (string.IsNullOrWhiteSpace(entra.Rpe) || entra.Rol is null) return BadRequest(Error("Elige al profesor que entra y su rol."));
            if (!await Titulacion.Vigentes(db).AnyAsync(p => p.Rpe == entra.Rpe)) return BadRequest(Error("Elige un profesor de la lista."));
        }

        // El comité como quedaría: 1 director de planta, máximo 1 codirector y 2 sinodales, sin repetir profesor.
        var queda = vigentes.Where(m => m != actual).Select(m => (m.Rpe, Rol: m.IdRolComite)).ToList();
        if (entra.Rpe is not null) queda.Add((entra.Rpe, entra.Rol!.Value));
        if (queda.Select(m => m.Rpe).Distinct().Count() != queda.Count) return BadRequest(Error("Ese profesor ya está en el comité."));
        if (queda.Count(m => m.Rol == RolComite.Director) != 1) return BadRequest(Error("El comité lleva un director."));
        if (queda.Count(m => m.Rol == RolComite.Codirector) > 1) return BadRequest(Error("El comité ya tiene codirector."));
        if (queda.Count(m => m.Rol == RolComite.Sinodal) > 2) return BadRequest(Error("El comité ya tiene dos sinodales."));
        var director = queda.Single(m => m.Rol == RolComite.Director).Rpe;
        if (await db.ProfesorDetalle.AnyAsync(d => d.Rpe == director && d.Externo)) return BadRequest(Error("El director debe ser profesor de planta."));

        if (actual is not null)
        {
            actual.Activo = false;
            actual.FechaBaja = fecha;
        }
        if (entra.Rpe is not null)
            db.ComiteProfesor.Add(new ComiteProfesor { IdComite = id, Rpe = entra.Rpe, IdRolComite = entra.Rol!.Value, FechaAsignacion = fecha, Activo = true });
        Titulacion.RegistrarHistorial(db, comite,
            actual is null ? [] : [(actual.Rpe, actual.IdRolComite)], entra.Rpe is null ? [] : [(entra.Rpe, entra.Rol!.Value)], fecha, User.IdUsuario());
        tesis.RpeDirector = director;
        tesis.UltimaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Con un informe en curso: la revisión de quien sale se descarta y el informe se reajusta (puede volver a Abierta).
        var sale = actual is not null && actual.Rpe != entra.Rpe ? [actual.Rpe] : new List<string>();
        await Titulacion.AjustarInformeTrasCambio(db, tesis.IdTesis, sale);
        await tx.CommitAsync();
        return NoContent();
    }

    // CM4: profesores para elegir.
    [HttpGet("participacion")]
    public async Task<IActionResult> Profesores()
    {
        var profesores = await db.Profesor.AsNoTracking().Select(p => new { p.Rpe, p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno }).ToListAsync();
        return Ok(profesores.Select(p => new { rpe = p.Rpe, nombre = Titulacion.NombreCompleto(p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno) }).OrderBy(p => p.nombre));
    }

    // CM4 (RF-03.5): las filas de un profesor, con su comité, rol y estado.
    [HttpGet("participacion/{rpe}")]
    public async Task<IActionResult> Participacion(string rpe)
    {
        var p = await db.Profesor.AsNoTracking().Where(x => x.Rpe == rpe).Select(x => new
        {
            x.Nombre, x.ApellidoPaterno, x.ApellidoMaterno,
            Correo = x.ProfesorDetalle.Select(d => d.CorreoInstitucional ?? d.IdUsuarioNavigation!.Correo).FirstOrDefault(),
        }).SingleOrDefaultAsync();
        if (p is null) return NotFound();
        var filas = await db.ComiteProfesor.AsNoTracking().Where(m => m.Rpe == rpe).OrderByDescending(m => m.IdComiteNavigation.Clave)
            .Select(m => new
            {
                IdComite = m.IdComite, m.IdComiteNavigation.Clave, m.IdRolComite, m.Activo, m.FechaBaja,
                m.IdComiteNavigation.IdTesisNavigation.Titulo, m.IdComiteNavigation.IdTesisNavigation.IdEstadoTesis,
                m.IdComiteNavigation.IdTesisNavigation.ClaveAlumnoNavigation.Nombre, m.IdComiteNavigation.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoPaterno,
                m.IdComiteNavigation.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoMaterno,
            }).ToListAsync();

        var participaciones = filas.Select(f => new
        {
            idComite = f.IdComite, comite = f.Clave, alumno = Titulacion.NombreCompleto(f.Nombre, f.ApellidoPaterno, f.ApellidoMaterno), trabajo = f.Titulo,
            rol = Rol(f.IdRolComite),
            participacion = f.FechaBaja != null ? "Reemplazada" : f.IdEstadoTesis is EstadoTesis.Aceptada or EstadoTesis.Cancelada ? "Concluida" : "Activa",
        }).ToList();
        return Ok(new
        {
            rpe, nombre = Titulacion.NombreCompleto(p.Nombre, p.ApellidoPaterno, p.ApellidoMaterno), correo = p.Correo,
            activos = participaciones.Count(x => x.participacion == "Activa"),
            director = participaciones.Count(x => x.participacion == "Activa" && x.rol == "Director"),
            concluidos = participaciones.Count(x => x.participacion == "Concluida"),
            participaciones,
        });
    }
}
