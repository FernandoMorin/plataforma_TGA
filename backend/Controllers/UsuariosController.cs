using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record AltaUsuarioInput(
    [Required, RegularExpression("profesor|externo|alumno|coordinador|administrador")] string Tipo,
    [Required, EmailAddress, MaxLength(100)] string Correo,
    [Required, MaxLength(50)] string Nombre,
    [Required, MaxLength(50)] string ApellidoPaterno,
    [MaxLength(50)] string? ApellidoMaterno,
    [MaxLength(10)] string? Rpe,
    [MaxLength(150)] string? Institucion,
    DateTime? VigenteHasta,
    int? ClaveAlumno,
    int? IdGeneracion,
    int? IdPeriodo);

public record EdicionUsuarioInput([Required, EmailAddress, MaxLength(100)] string Correo, [Required] string Rol, bool Activo);

// Gestión de usuarios (AMyD RF-07; Figma U1-U4). Aquí se dan de alta profesores, externos, alumnos temporales,
// coordinadores y administradores; los alumnos se importan y los egresados se dan de alta en Egresados.
[ApiController, Route("api/usuarios"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public partial class UsuariosController(TgaDbContext db, Correo correos, UniversidadExterna universidad) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    [GeneratedRegex(@"^\d{1,10}$")]
    private static partial Regex FormatoRpe();

    // U1: todas las cuentas. El estado se calcula: inactiva, bloqueada, externo vencido o por vencer, activa.
    // shortcut: sin paginar en el servidor, como la consulta de alumnos; paginar si llegan a miles.
    [HttpGet]
    public async Task<IActionResult> Consulta()
    {
        var filas = await db.Usuario.AsNoTracking().OrderBy(u => u.Nombre).Select(u => new
        {
            u.IdUsuario, u.Nombre, u.Correo, u.ClaveInstitucional, u.Activo, u.BloqueadoHasta, u.UltimoAcceso,
            Rol = u.IdRolUsuarioNavigation!.Nombre,
            Externo = u.ProfesorDetalle.Where(p => p.Externo).Select(p => new { p.VigenteHasta, p.Institucion }).FirstOrDefault(),
        }).ToListAsync();

        return Ok(filas.Select(u => new
        {
            id = u.IdUsuario,
            nombre = u.Nombre,
            correo = u.Correo,
            clave = u.ClaveInstitucional,
            rol = u.Rol,
            externo = u.Externo is not null,
            grupo = u.Externo is not null ? "Externos"
                : u.Rol is Roles.Coordinador or Roles.Administrador ? "Coordinación"
                : u.Rol == Roles.Profesor ? "Profesores"
                : u.Rol == Roles.Alumno ? "Alumnos" : "Egresados",
            estado = Estado(u.Activo, u.BloqueadoHasta, u.Externo?.VigenteHasta),
            ultimoAcceso = u.UltimoAcceso,
        }));
    }

    static object Estado(bool? activo, DateTime? bloqueadoHasta, DateTime? vigenteHasta) =>
        activo != true || vigenteHasta < DateTime.Today ? new { tipo = "Inactivo", hasta = (DateTime?)null }
        : bloqueadoHasta > DateTime.UtcNow ? new { tipo = "Bloqueado", hasta = bloqueadoHasta }
        : vigenteHasta is not null ? new { tipo = "Vence", hasta = vigenteHasta }
        : new { tipo = "Activo", hasta = (DateTime?)null };

    // Roles a los que puede pasar una cuenta sin romper sus datos: el coordinador también es profesor (necesita RPE),
    // el externo solo es profesor, y alumnos y egresados conservan su rol.
    static string[] RolesPermitidos(string rol, bool profesor, bool externo) =>
        externo ? [Roles.Profesor]
        : rol is Roles.Alumno or Roles.Egresado ? [rol]
        : profesor ? [Roles.Profesor, Roles.Coordinador, Roles.Administrador]
        : [Roles.Administrador];

    // Detalle de una cuenta: para el diálogo de edición (U3) y el encabezado del registro de accesos (U4).
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var u = await db.Usuario.AsNoTracking().Where(x => x.IdUsuario == id).Select(x => new
        {
            x.IdUsuario, x.Nombre, x.Correo, x.ClaveInstitucional, x.Activo, x.BloqueadoHasta,
            Rol = x.IdRolUsuarioNavigation!.Nombre,
            Profesor = x.ProfesorDetalle.Select(p => new { p.Externo, p.VigenteHasta, p.Institucion }).FirstOrDefault(),
        }).SingleOrDefaultAsync();
        if (u is null) return NotFound();
        var externo = u.Profesor?.Externo == true;
        return Ok(new
        {
            id = u.IdUsuario, nombre = u.Nombre, correo = u.Correo, clave = u.ClaveInstitucional, rol = u.Rol,
            activo = u.Activo == true, externo, institucion = u.Profesor?.Institucion,
            estado = Estado(u.Activo, u.BloqueadoHasta, externo ? u.Profesor!.VigenteHasta : null),
            bloqueadoHasta = u.BloqueadoHasta > DateTime.UtcNow ? u.BloqueadoHasta : null,
            rolesPermitidos = RolesPermitidos(u.Rol, u.Profesor is not null, externo),
        });
    }

    // U4: registro de accesos. Solo el administrador.
    [HttpGet("{id:int}/accesos"), Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Accesos(int id) =>
        Ok(await db.LogAcceso.AsNoTracking().Where(l => l.IdUsuario == id).OrderByDescending(l => l.Fecha).Take(200)
            .Select(l => new { fecha = l.Fecha, resultado = l.Resultado, ip = l.Ip }).ToListAsync());

    // Clave que recibirá el siguiente profesor externo (U2b muestra «Clave asignada»).
    [HttpGet("siguiente-externo")]
    public async Task<IActionResult> SiguienteExterno() => Ok(new { clave = await ClaveExterno() });

    async Task<string> ClaveExterno()
    {
        var usadas = await db.Profesor.Where(p => p.Rpe.StartsWith("EXT-")).Select(p => p.Rpe).ToListAsync();
        var mayor = usadas.Select(c => int.TryParse(c[4..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"EXT-{mayor + 1:000}";
    }

    // U2: alta. La contraseña inicial la genera el sistema y se manda al correo de la cuenta; si el correo
    // no sale, el alta se deshace, porque sin contraseña la cuenta no le sirve a nadie.
    [HttpPost]
    public async Task<IActionResult> Alta(AltaUsuarioInput input)
    {
        if (!correos.Configurado) return StatusCode(503, Error("El envío de correos no está configurado."));
        var correo = input.Correo.Trim().ToLowerInvariant();
        var nombre = input.Nombre.Trim();
        var paterno = input.ApellidoPaterno.Trim();
        var materno = string.IsNullOrWhiteSpace(input.ApellidoMaterno) ? null : input.ApellidoMaterno.Trim();
        var completo = string.Join(' ', new[] { nombre, paterno, materno }.Where(s => s is not null));
        if (await db.Usuario.AnyAsync(u => u.Correo == correo)) return Conflict(Error("Ese correo ya tiene una cuenta."));

        var rolNombre = input.Tipo switch
        {
            "coordinador" => Roles.Coordinador, "administrador" => Roles.Administrador, "alumno" => Roles.Alumno, _ => Roles.Profesor,
        };
        var rol = await db.CatRolUsuario.Where(r => r.Nombre == rolNombre).Select(r => r.IdRolUsuario).SingleAsync();
        var contrasena = Seguridad.ContrasenaInicial();
        var usuario = new Usuario
        {
            Correo = correo, IdRolUsuario = rol, Nombre = completo, Activo = true, RequiereCambio = true,
            IntentosFallidos = 0, FechaCreacion = DateTime.UtcNow,
        };
        usuario.ContrasenaHash = Seguridad.Hashear(usuario, contrasena);

        await using var tx = await db.Database.BeginTransactionAsync();
        switch (input.Tipo)
        {
            case "profesor" or "coordinador":
            {
                var rpe = input.Rpe?.Trim() ?? "";
                if (!FormatoRpe().IsMatch(rpe)) return BadRequest(Error("El RPE son solo números, hasta 10."));
                if (await db.Profesor.AnyAsync(p => p.Rpe == rpe) || await db.Usuario.AnyAsync(u => u.ClaveInstitucional == rpe))
                    return Conflict(Error("Ese RPE ya está registrado."));
                usuario.ClaveInstitucional = rpe;
                db.ProfesorDetalle.Add(new ProfesorDetalle
                {
                    RpeNavigation = new Profesor { Rpe = rpe, Nombre = nombre, ApellidoPaterno = paterno, ApellidoMaterno = materno },
                    IdUsuarioNavigation = usuario, CorreoInstitucional = correo, Activo = true,
                });
                break;
            }
            case "externo":
            {
                if (string.IsNullOrWhiteSpace(input.Institucion)) return BadRequest(Error("Falta la institución de procedencia."));
                if (input.VigenteHasta is not DateTime vigencia || vigencia.Date < DateTime.Today) return BadRequest(Error("La vigencia debe ser hoy o una fecha futura."));
                var clave = await ClaveExterno();
                usuario.ClaveInstitucional = clave;
                db.ProfesorDetalle.Add(new ProfesorDetalle
                {
                    RpeNavigation = new Profesor { Rpe = clave, Nombre = nombre, ApellidoPaterno = paterno, ApellidoMaterno = materno },
                    IdUsuarioNavigation = usuario, CorreoInstitucional = correo, Activo = true,
                    Externo = true, Institucion = input.Institucion.Trim(), VigenteHasta = vigencia.Date,
                });
                break;
            }
            case "alumno":
            {
                // Alumno temporal: alumno real que todavía no aparece en el registro de la universidad.
                if (input.ClaveAlumno is not (>= UniversidadExterna.ClaveMinima and <= UniversidadExterna.ClaveMaxima) || input.ClaveAlumno is not int claveAlumno)
                    return BadRequest(Error("La clave del alumno son 6 números."));
                if (await db.Alumno.AnyAsync(a => a.ClaveAlumno == claveAlumno)) return Conflict(Error("Esa clave ya está en el sistema."));
                if (universidad.Configurado)
                {
                    try
                    {
                        if ((await universidad.Buscar([claveAlumno], null)).Count > 0)
                            return Conflict(Error("Esa clave sí está en el registro de la universidad: impórtala desde Importar alumnos."));
                    }
                    catch (MySqlException) { /* Sin registro disponible no se puede comprobar; se registra como temporal. */ }
                }
                var periodo = await db.PeriodoAcademico.Where(p => p.IdPeriodo == input.IdPeriodo).Select(p => new { p.IdPeriodo, p.FechaInicio }).SingleOrDefaultAsync();
                if (periodo is null || !await db.Generacion.AnyAsync(g => g.IdGeneracion == input.IdGeneracion))
                    return BadRequest(Error("Elige la generación y el periodo de ingreso."));
                usuario.ClaveInstitucional = claveAlumno.ToString();
                db.Alumno.Add(new Alumno
                {
                    ClaveAlumno = claveAlumno, Nombre = nombre, ApellidoPaterno = paterno, ApellidoMaterno = materno,
                    CorreoInstitucional = correo, IdUsuarioNavigation = usuario, PendienteConciliar = true,
                    IdPrograma = await db.CatProgramaAcademico.OrderBy(p => p.IdPrograma).Select(p => p.IdPrograma).FirstAsync(),
                    IdGeneracion = input.IdGeneracion, IdEstadoAcademico = EstadoAcademico.Activo,
                    FechaIngreso = periodo.FechaInicio ?? DateTime.Today,
                    AlumnoDetalle = [new AlumnoDetalle { IdPeriodo = periodo.IdPeriodo, Semestre = 1, FechaRegistro = DateTime.Today }],
                });
                break;
            }
            default:
                db.Usuario.Add(usuario);
                break;
        }

        await db.SaveChangesAsync();
        try { await correos.EnviarContrasena(correo, nombre, contrasena, egresado: false); }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or FormatException)
        {
            await tx.RollbackAsync();
            return StatusCode(502, Error("No se pudo enviar el correo con la contraseña. Revisa la configuración de correo."));
        }
        await tx.CommitAsync();
        return Ok(new { id = usuario.IdUsuario, clave = usuario.ClaveInstitucional });
    }

    // U3: editar correo, rol y si la cuenta está activa.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, EdicionUsuarioInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = await db.Usuario.Include(x => x.IdRolUsuarioNavigation).Include(x => x.ProfesorDetalle).Include(x => x.Alumno).ThenInclude(a => a.Egresado)
            .SingleOrDefaultAsync(x => x.IdUsuario == id);
        if (u is null) return NotFound();
        var rolActual = u.IdRolUsuarioNavigation!.Nombre;
        if (id == User.IdUsuario() && (!input.Activo || input.Rol != rolActual))
            return BadRequest(Error("No puedes desactivar tu propia cuenta ni cambiar tu rol."));
        var profesor = u.ProfesorDetalle.FirstOrDefault();
        if (!RolesPermitidos(rolActual, profesor is not null, profesor?.Externo == true).Contains(input.Rol))
            return BadRequest(Error("Esa cuenta no puede tener ese rol."));

        var correo = input.Correo.Trim().ToLowerInvariant();
        if (correo != u.Correo)
        {
            if (await db.Usuario.AnyAsync(x => x.Correo == correo && x.IdUsuario != id)) return Conflict(Error("Ese correo ya tiene una cuenta."));
            // El correo de la cuenta es el mismo que se guarda en su ficha de alumno, egresado o profesor.
            u.Correo = correo;
            foreach (var a in u.Alumno)
            {
                if (rolActual == Roles.Egresado && a.Egresado is not null) a.Egresado.Correo = correo;
                else a.CorreoInstitucional = correo;
            }
            foreach (var p in u.ProfesorDetalle) p.CorreoInstitucional = correo;
        }
        u.IdRolUsuario = await db.CatRolUsuario.Where(r => r.Nombre == input.Rol).Select(r => r.IdRolUsuario).SingleAsync();
        u.Activo = input.Activo;
        if (!input.Activo)
        {
            await Seguridad.CerrarSesiones(db, id);
            await Seguridad.InvalidarTokens(db, id);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var n = await db.Usuario.Where(u => u.IdUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.BloqueadoHasta, (DateTime?)null).SetProperty(u => u.IntentosFallidos, 0));
        return n == 0 ? NotFound() : NoContent();
    }

    // U3: contraseña temporal nueva, enviada por correo. La cuenta deberá cambiarla al entrar.
    [HttpPost("{id:int}/contrasena-temporal")]
    public async Task<IActionResult> ContrasenaTemporal(int id)
    {
        if (!correos.Configurado) return StatusCode(503, Error("El envío de correos no está configurado."));
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = await db.Usuario.Include(x => x.IdRolUsuarioNavigation).SingleOrDefaultAsync(x => x.IdUsuario == id);
        if (u is null) return NotFound();
        var contrasena = Seguridad.ContrasenaInicial();
        u.ContrasenaHash = Seguridad.Hashear(u, contrasena);
        u.RequiereCambio = true;
        u.IntentosFallidos = 0;
        u.BloqueadoHasta = null;
        await Seguridad.CerrarSesiones(db, id);
        await Seguridad.InvalidarTokens(db, id);
        await db.SaveChangesAsync();
        try { await correos.EnviarContrasena(u.Correo, u.Nombre ?? u.Correo, contrasena, egresado: u.IdRolUsuarioNavigation!.Nombre == Roles.Egresado); }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or FormatException)
        {
            await tx.RollbackAsync();
            return StatusCode(502, Error("No se pudo enviar el correo con la contraseña. Revisa la configuración de correo."));
        }
        await tx.CommitAsync();
        return NoContent();
    }
}
