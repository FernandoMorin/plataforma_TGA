using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record IntentoInput(DateTime Fecha, [Required, MaxLength(1000)] string Observaciones);
public record RevisionConstanciaInput(bool Revisada);
public record AltaEgresadoInput(int Clave, int IdGeneracion, DateTime FechaEgreso, [Required, EmailAddress, MaxLength(100)] string Correo, [MaxLength(20)] string? Telefono);

// Egresados del coordinador (Figma E1 consulta, E2-E3 ficha, E4 alta).
[ApiController, Route("api/egresados"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public class EgresadosController(TgaDbContext db, UniversidadExterna universidad, Correo correos, ILogger<EgresadosController> logger) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    // E1
    [HttpGet]
    public async Task<IActionResult> Consulta()
    {
        var egresados = await db.Egresado.AsNoTracking().Select(e => new
        {
            e.ClaveAlumno, e.IdentificadorEgresado, e.IdEstadoEgresado, e.UltimaActualizacion, e.Activo,
            e.ClaveAlumnoNavigation.Nombre, e.ClaveAlumnoNavigation.ApellidoPaterno, e.ClaveAlumnoNavigation.ApellidoMaterno,
            Generacion = e.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
        }).ToListAsync();
        var claves = egresados.Select(e => e.ClaveAlumno).ToList();
        var empleos = (await db.Empleo.AsNoTracking().Where(x => claves.Contains(x.ClaveAlumno)).Select(x => new
        {
            x.ClaveAlumno, Vista = new EmpleoVista(x.IdEmpleo, x.Tipo, x.DetalleEmpleo.Select(d => d.Puesto).FirstOrDefault(), x.IdEmpresaNavigation!.Nombre,
                x.AreaProfesional, x.DetalleEmpleo.Select(d => d.FechaInicio).FirstOrDefault(), x.DetalleEmpleo.Select(d => d.FechaFinal).FirstOrDefault(),
                x.DetalleEmpleo.Select(d => d.RelacionPosgrado).FirstOrDefault(), null),
        }).ToListAsync()).ToLookup(x => x.ClaveAlumno, x => x.Vista);

        return Ok(egresados.OrderBy(e => e.Nombre).ThenBy(e => e.ApellidoPaterno).Select(e =>
        {
            var suyos = empleos[e.ClaveAlumno].ToList();
            var actual = Egresados.Actual(suyos);
            return new
            {
                clave = e.ClaveAlumno, identificador = e.IdentificadorEgresado, generacion = e.Generacion,
                nombre = Titulacion.NombreCompleto(e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno),
                situacion = Egresados.Situacion(suyos),
                empleoActual = actual is null ? null : string.Join(" · ", new[] { actual.Puesto, actual.Empresa }.Where(x => !string.IsNullOrWhiteSpace(x))),
                // Para los filtros: áreas y relación con la maestría de los empleos vigentes.
                areas = suyos.Where(x => x.Vigente && !string.IsNullOrWhiteSpace(x.Area)).Select(x => x.Area!.Trim()).Distinct(),
                relacion = actual?.Relacion is null ? null : actual.Relacion.Value ? "Sí" : "No",
                actualizado = e.UltimaActualizacion, sinActualizar = Egresados.SinActualizar(e.UltimaActualizacion),
                estado = e.IdEstadoEgresado == Egresados.NoLocalizable ? "No localizable" : "Localizable",
                activo = e.Activo,
            };
        }));
    }

    // E2-E3: ficha con resumen, historial laboral, intentos de contacto, comentarios y constancias.
    [HttpGet("{clave:int}")]
    public async Task<IActionResult> Ficha(int clave)
    {
        var e = await db.Egresado.AsNoTracking().Where(x => x.ClaveAlumno == clave).Select(x => new
        {
            x.IdentificadorEgresado, x.FechaEgreso, x.IdEstadoEgresado, x.Correo, x.Telefono, x.Ciudad, x.OtrosContactos, x.UltimaActualizacion, x.Activo,
            x.ClaveAlumnoNavigation.Nombre, x.ClaveAlumnoNavigation.ApellidoPaterno, x.ClaveAlumnoNavigation.ApellidoMaterno,
            Generacion = x.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
        }).SingleOrDefaultAsync();
        if (e is null) return NotFound();

        var empleos = await Egresados.Empleos(db, clave).ToListAsync();
        var (meses, rango) = Egresados.TiempoParaEmplearse(e.FechaEgreso, empleos);
        var actual = Egresados.Actual(empleos);
        var intentos = await db.IntentoContacto.AsNoTracking().Where(i => i.ClaveAlumno == clave)
            .OrderByDescending(i => i.FechaIntento).ThenByDescending(i => i.IdIntento)
            .Select(i => new { fecha = i.FechaIntento, observaciones = i.Observaciones }).ToListAsync();
        var comentarios = await db.RetroalimentacionEgresado.AsNoTracking().Where(r => r.ClaveAlumno == clave)
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.IdRetroalimentacion)
            .Select(r => new { fecha = r.Fecha, comentario = r.Comentario, sugerencia = r.Sugerencia }).ToListAsync();
        var constancias = await db.Documento.AsNoTracking().Where(d => d.ClaveAlumno == clave && d.IdTipoDocumento == TipoDocumento.Constancia)
            .OrderByDescending(d => d.FechaCarga).Select(d => new { id = d.IdDocumento, nombre = d.NombreArchivo, fecha = d.FechaCarga, revisada = d.Verificado }).ToListAsync();
        var noLocalizable = e.IdEstadoEgresado == Egresados.NoLocalizable;

        return Ok(new
        {
            clave, identificador = e.IdentificadorEgresado, generacion = e.Generacion, egreso = e.FechaEgreso,
            nombre = Titulacion.NombreCompleto(e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno),
            estado = noLocalizable ? "No localizable" : "Localizable", activo = e.Activo,
            contacto = new { correo = e.Correo, telefono = e.Telefono, ciudad = e.Ciudad, otros = e.OtrosContactos, actualizado = e.UltimaActualizacion },
            situacion = Egresados.Situacion(empleos), tiempo = meses, rango,
            actual = actual is null ? null : new { empresa = actual.Empresa, puesto = actual.Puesto, area = actual.Area, desde = actual.Inicio, relacion = actual.Relacion },
            empleos = empleos.OrderByDescending(x => x.Vigente).ThenByDescending(x => x.Inicio)
                .Select(x => new { id = x.Id, tipo = x.Tipo, puesto = x.Puesto, empresa = x.Empresa, area = x.Area, inicio = x.Inicio, fin = x.Fin, relacion = x.Relacion, vigente = x.Vigente }),
            intentos, comentarios, constancias,
            // Con 3 intentos sin respuesta el sistema sugiere marcarlo no localizable; el coordinador confirma.
            sugerirNoLocalizable = !noLocalizable && intentos.Count >= 3,
        });
    }

    [HttpPost("{clave:int}/intentos")]
    public async Task<IActionResult> RegistrarIntento(int clave, IntentoInput input)
    {
        if (input.Fecha.Date > DateTime.Today) return BadRequest(Error("La fecha no puede ser futura."));
        var observaciones = input.Observaciones.Trim();
        if (observaciones.Length == 0) return BadRequest(Error("Escribe el medio usado y el resultado."));
        if (!await db.Egresado.AnyAsync(e => e.ClaveAlumno == clave)) return NotFound();
        db.IntentoContacto.Add(new IntentoContacto { ClaveAlumno = clave, FechaIntento = input.Fecha.Date, Observaciones = observaciones });
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Confirmación del coordinador (RF-02.11). Vuelve a Localizable cuando el egresado actualiza sus datos.
    [HttpPost("{clave:int}/no-localizable")]
    public async Task<IActionResult> MarcarNoLocalizable(int clave) =>
        await Actualizar(clave, e => e.IdEstadoEgresado = Egresados.NoLocalizable);

    // Baja lógica (RF-02.6) y su reversa: no se borra nada.
    [HttpPost("{clave:int}/baja")]
    public Task<IActionResult> Baja(int clave) => Actualizar(clave, e => e.Activo = false);

    [HttpPost("{clave:int}/reactivar")]
    public Task<IActionResult> Reactivar(int clave) => Actualizar(clave, e => e.Activo = true);

    async Task<IActionResult> Actualizar(int clave, Action<Egresado> cambio)
    {
        var e = await db.Egresado.SingleOrDefaultAsync(x => x.ClaveAlumno == clave);
        if (e is null) return NotFound();
        cambio(e);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("constancias/{id:int}")]
    public async Task<IActionResult> RevisarConstancia(int id, RevisionConstanciaInput input)
    {
        var d = await db.Documento.SingleOrDefaultAsync(x => x.IdDocumento == id && x.IdTipoDocumento == TipoDocumento.Constancia);
        if (d is null) return NotFound();
        d.Verificado = input.Revisada;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // E4: busca la clave en el sistema (alumno Titulado) o en el registro de la universidad (estatus Egresado).
    async Task<(string Nombre, string Paterno, string? Materno, string? CorreoInstitucional, int? IdGeneracion, bool EnSistema)?> Candidato(int clave)
    {
        var a = await db.Alumno.AsNoTracking().Where(x => x.ClaveAlumno == clave)
            .Select(x => new { x.Nombre, x.ApellidoPaterno, x.ApellidoMaterno, x.CorreoInstitucional, x.IdGeneracion, x.IdEstadoAcademico }).SingleOrDefaultAsync();
        if (a is not null)
            return a.IdEstadoAcademico == EstadoAcademico.Titulado ? (a.Nombre ?? "", a.ApellidoPaterno ?? "", a.ApellidoMaterno, a.CorreoInstitucional, a.IdGeneracion, true) : null;
        if (!universidad.Configurado) return null;
        var r = (await universidad.Buscar([clave], null)).SingleOrDefault();
        return r?.Estatus == "Egresado" ? (r.Nombre, r.ApellidoPaterno, r.ApellidoMaterno, r.Correo, null, false) : null;
    }

    [HttpGet("alta/buscar/{clave:int}")]
    public async Task<IActionResult> Buscar(int clave)
    {
        if (await db.Egresado.AnyAsync(e => e.ClaveAlumno == clave)) return Conflict(Error("Ya está registrado como egresado."));
        try
        {
            var c = await Candidato(clave);
            if (c is null) return NotFound(Error("No aparece como egresado en el registro de la universidad ni como titulado en el sistema."));
            return Ok(new
            {
                clave, nombre = Titulacion.NombreCompleto(c.Value.Nombre, c.Value.Paterno, c.Value.Materno), correo = c.Value.CorreoInstitucional,
                idGeneracion = c.Value.IdGeneracion, origen = c.Value.EnSistema ? "Titulado en el sistema" : "Egresado en la universidad",
            });
        }
        catch (MySqlException ex)
        {
            logger.LogError(ex, "No se pudo consultar el registro de la universidad");
            return StatusCode(502, Error("No se pudo consultar el registro de la universidad."));
        }
    }

    // Vista previa del identificador según la generación elegida.
    [HttpGet("alta/identificador/{clave:int}/{idGeneracion:int}")]
    public async Task<IActionResult> VistaIdentificador(int clave, int idGeneracion)
    {
        var anio = await db.Generacion.Where(g => g.IdGeneracion == idGeneracion).Select(g => g.AnioIngreso).SingleOrDefaultAsync();
        var c = await Candidato(clave);
        if (anio is null || c is null) return NotFound();
        return Ok(new { identificador = await Egresados.Identificador(db, anio.Value, c.Value.Nombre, c.Value.Paterno, c.Value.Materno) });
    }

    // E4: crea (si falta) su fila de alumno, el egresado y su cuenta con correo personal; la contraseña va por correo.
    [HttpPost("alta")]
    public async Task<IActionResult> Alta(AltaEgresadoInput input)
    {
        if (input.FechaEgreso.Date > DateTime.Today) return BadRequest(Error("La fecha de egreso no puede ser futura."));
        var correo = input.Correo.Trim().ToLowerInvariant();
        var anio = await db.Generacion.Where(g => g.IdGeneracion == input.IdGeneracion).Select(g => g.AnioIngreso).SingleOrDefaultAsync();
        if (anio is null) return BadRequest(Error("Elige la generación."));
        if (await db.Usuario.AnyAsync(u => u.Correo == correo)) return Conflict(Error("Ese correo ya tiene una cuenta."));

        (string Nombre, string Paterno, string? Materno, string? CorreoInstitucional, int? IdGeneracion, bool EnSistema)? c;
        try { c = await Candidato(input.Clave); }
        catch (MySqlException ex)
        {
            logger.LogError(ex, "No se pudo consultar el registro de la universidad");
            return StatusCode(502, Error("No se pudo consultar el registro de la universidad."));
        }
        if (c is null) return BadRequest(Error("No aparece como egresado en el registro de la universidad ni como titulado en el sistema."));
        var (nombre, paterno, materno, institucional, _, enSistema) = c.Value;

        await using var tx = await db.Database.BeginTransactionAsync();
        if (await db.Egresado.AnyAsync(e => e.ClaveAlumno == input.Clave)) return Conflict(Error("Ya está registrado como egresado."));
        if (!enSistema)
        {
            var programa = await db.CatProgramaAcademico.OrderBy(p => p.IdPrograma).Select(p => p.IdPrograma).FirstAsync();
            db.Alumno.Add(new Alumno
            {
                ClaveAlumno = input.Clave, Nombre = nombre, ApellidoPaterno = paterno, ApellidoMaterno = materno, CorreoInstitucional = institucional,
                IdPrograma = programa, IdGeneracion = input.IdGeneracion, IdEstadoAcademico = EstadoAcademico.Titulado,
            });
        }
        var identificador = await Egresados.Identificador(db, anio.Value, nombre, paterno, materno);
        var rolEgresado = await db.CatRolUsuario.Where(r => r.Nombre == Roles.Egresado).Select(r => r.IdRolUsuario).SingleAsync();
        var nombreCompleto = Titulacion.NombreCompleto(nombre, paterno, materno);
        var contrasena = Seguridad.ContrasenaInicial();
        // El egresado entra con su correo personal; su clave institucional es el identificador (como en el seeder).
        var usuario = new Usuario
        {
            Correo = correo, IdRolUsuario = rolEgresado, Activo = true, RequiereCambio = true, IntentosFallidos = 0,
            FechaCreacion = DateTime.UtcNow, Nombre = nombreCompleto, ClaveInstitucional = identificador,
        };
        usuario.ContrasenaHash = Seguridad.Hashear(usuario, contrasena);
        db.Usuario.Add(usuario);
        db.Egresado.Add(new Egresado
        {
            ClaveAlumno = input.Clave, IdentificadorEgresado = identificador, FechaEgreso = input.FechaEgreso.Date, IdEstadoEgresado = Egresados.Localizable,
            Correo = correo, Telefono = string.IsNullOrWhiteSpace(input.Telefono) ? null : input.Telefono.Trim(), UltimaActualizacion = DateTime.Today, Activo = true,
        });
        await db.SaveChangesAsync();

        try { await correos.EnviarContrasena(correo, nombreCompleto, contrasena, egresado: true); }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or FormatException)
        {
            logger.LogError(ex, "No se pudo enviar la contraseña del egresado {Clave}", input.Clave);
            return StatusCode(502, Error("No se pudo enviar el correo con la contraseña. Revisa la configuración de correo."));
        }
        await tx.CommitAsync();
        return Ok(new { identificador, nombre = nombreCompleto });
    }
}
