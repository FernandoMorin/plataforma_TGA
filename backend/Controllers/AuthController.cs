using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record RecuperarInput([Required, EmailAddress, MaxLength(100)] string Correo);
public record RestablecerInput([Required, StringLength(64, MinimumLength = 64)] string Token, [Required, MaxLength(128)] string Contrasena);

public record LoginInput(
    [Required, EmailAddress, MaxLength(100)] string Correo,
    [Required, MaxLength(128)] string Contrasena,
    bool PortalEgresados = false);

[ApiController, Route("api/auth")]
public class AuthController(TgaDbContext db, Correo correos, ILogger<AuthController> logger) : ControllerBase
{
    // Respuestas con un código que el frontend traduce al aviso de la maqueta.
    static object Codigo(string codigo) => new { codigo };

    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var correo = input.Correo.Trim().ToLowerInvariant();
        var ahora = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        // FOR UPDATE: dos intentos simultáneos no pueden saltarse el conteo de fallos.
        var u = (await db.Usuario.FromSqlInterpolated($"SELECT * FROM usuario WHERE correo = {correo} FOR UPDATE")
            .Include(x => x.IdRolUsuarioNavigation).ToListAsync()).SingleOrDefault();

        if (u is null || u.Activo != true || await Seguridad.ExternoVencido(db, u.IdUsuario))
        {
            // shortcut: cuenta inexistente o inactiva se registra como «Contraseña incorrecta» para no revelar cuál es.
            await Registrar(u, "Contraseña incorrecta", tx);
            return Unauthorized(Codigo("credenciales"));
        }
        if (u.BloqueadoHasta > ahora)
        {
            await Registrar(u, "Bloqueado", tx);
            return StatusCode(423, Codigo("bloqueada"));
        }

        if (!Seguridad.Verificar(u, input.Contrasena))
        {
            u.IntentosFallidos = (u.IntentosFallidos ?? 0) + 1;
            if (u.IntentosFallidos >= Seguridad.Intentos)
            {
                u.IntentosFallidos = 0;
                u.BloqueadoHasta = ahora.AddMinutes(Seguridad.BloqueoMinutos);
                await Seguridad.CerrarSesiones(db, u.IdUsuario);
                await Registrar(u, "Bloqueado", tx);
                return StatusCode(423, Codigo("bloqueada"));
            }
            await Registrar(u, "Contraseña incorrecta", tx);
            return Unauthorized(Codigo("credenciales"));
        }

        // Egresados solo por su portal y los demás solo por el general; si no, se responde
        // igual que con credenciales incorrectas para no revelar que la cuenta existe.
        var rol = u.IdRolUsuarioNavigation!.Nombre;
        if ((rol == Roles.Egresado) != input.PortalEgresados)
        {
            await tx.CommitAsync();
            return Unauthorized(Codigo("credenciales"));
        }

        u.IntentosFallidos = 0;
        u.BloqueadoHasta = null;
        u.UltimoAcceso = ahora;
        var sid = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var anterior = User.FindFirstValue(Seguridad.ClaimSesion);
        if (anterior != null) await db.SesionUsuario.Where(s => s.Id == anterior).ExecuteDeleteAsync();
        db.SesionUsuario.Add(new SesionUsuario { Id = sid, UsuarioId = u.IdUsuario, Creada = ahora, UltimaActividad = ahora });
        await Registrar(u, u.RequiereCambio == true ? "Primer acceso" : "Correcto", tx);

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, u.IdUsuario.ToString()),
            new(ClaimTypes.Email, u.Correo),
            new(ClaimTypes.Role, rol),
            new(Seguridad.ClaimSesion, sid),
            .. u.RequiereCambio == true ? new Claim[] { new(Seguridad.ClaimCambio, "1") } : [],
        ];
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = ahora.AddHours(Seguridad.SesionMaximaHoras) });
        return Ok(new { rol, requiereCambio = u.RequiereCambio == true });
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var u = await db.Usuario.AsNoTracking().Where(x => x.IdUsuario == User.IdUsuario()).Select(x => new
        {
            x.Nombre, x.Correo, x.ClaveInstitucional, x.RequiereCambio,
            // Segundo acceso (Figma P3): el alumno completa su correo personal y su teléfono.
            SinContacto = x.Alumno.Any(a => a.CorreoPersonal == null || a.Telefono == null),
        }).SingleAsync();
        var rol = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new
        {
            u.Nombre, u.Correo, u.ClaveInstitucional, rol,
            requiereCambio = u.RequiereCambio == true,
            completarDatos = rol == Roles.Alumno && u.SinContacto,
            inactividadMinutos = Seguridad.InactividadMinutos,
        });
    }

    // El frontend lo llama mientras el usuario interactúa; sin llamadas, la sesión vence por inactividad.
    [Authorize, HttpPost("actividad")]
    public async Task<IActionResult> Actividad()
    {
        var sid = User.FindFirstValue(Seguridad.ClaimSesion);
        await db.SesionUsuario.Where(s => s.Id == sid).ExecuteUpdateAsync(s => s.SetProperty(x => x.UltimaActividad, DateTime.UtcNow));
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sid = User.FindFirstValue(Seguridad.ClaimSesion);
        if (sid != null) await db.SesionUsuario.Where(s => s.Id == sid).ExecuteDeleteAsync();
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    // Recuperación de acceso (Figma 05): misma respuesta exista o no la cuenta, para no revelar qué correos
    // están registrados. No se emite otro enlace a la misma cuenta antes de 2 minutos.
    const string RespuestaRecuperar = "Si la cuenta está activa, recibirás instrucciones por correo.";

    [HttpPost("recuperar"), EnableRateLimiting("recuperacion")]
    public async Task<IActionResult> Recuperar(RecuperarInput input)
    {
        if (!correos.Configurado) return StatusCode(503, new { mensaje = "La recuperación no está disponible. Contacta a la coordinación." });
        var correo = input.Correo.Trim().ToLowerInvariant();
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = (await db.Usuario.FromSqlInterpolated($"SELECT * FROM usuario WHERE correo = {correo} FOR UPDATE").ToListAsync()).SingleOrDefault();
        if (u?.Activo == true)
        {
            var ahora = DateTime.UtcNow;
            var reciente = await db.TokenRecuperacion.AnyAsync(t => t.IdUsuario == u.IdUsuario && t.Usado == false && t.FechaExpiracion > ahora.AddMinutes(28));
            if (!reciente)
            {
                await Seguridad.InvalidarTokens(db, u.IdUsuario);
                var token = Seguridad.Token();
                db.TokenRecuperacion.Add(new TokenRecuperacion { IdUsuario = u.IdUsuario, Token = Seguridad.HashToken(token), FechaExpiracion = ahora.AddMinutes(30), Usado = false });
                await db.SaveChangesAsync();
                try { await correos.EnviarRecuperacion(u.Correo, token); }
                catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or FormatException)
                {
                    // Sin el correo, el token no sirve: se descarta. Al usuario se le responde igual.
                    logger.LogWarning("No se pudo enviar un correo de recuperación. Revisa la configuración SMTP.");
                    await tx.RollbackAsync();
                    return Ok(new { mensaje = RespuestaRecuperar });
                }
            }
        }
        await tx.CommitAsync();
        return Ok(new { mensaje = RespuestaRecuperar });
    }

    // Restablecer con el enlace (Figma 06). El token es de un solo uso y vence a los 30 minutos;
    // al usarse se cierran todas las sesiones de la cuenta y se desbloquea.
    [HttpPost("restablecer"), EnableRateLimiting("recuperacion")]
    public async Task<IActionResult> Restablecer(RestablecerInput input)
    {
        if (!Seguridad.ContrasenaValida(input.Contrasena)) return BadRequest(new { mensaje = "Usa de 12 a 128 caracteres, con letras y números." });
        var hash = Seguridad.HashToken(input.Token);
        var dueno = await db.TokenRecuperacion.AsNoTracking().Where(t => t.Token == hash).Select(t => (int?)t.IdUsuario).SingleOrDefaultAsync();
        if (dueno is null) return BadRequest(new { mensaje = "El enlace no es válido o ya venció." });

        // Se bloquea la fila del usuario y se relee el token: dos usos simultáneos del mismo enlace no pasan.
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = (await db.Usuario.FromSqlInterpolated($"SELECT * FROM usuario WHERE id_usuario = {dueno.Value} FOR UPDATE").ToListAsync()).Single();
        var t = await db.TokenRecuperacion.SingleAsync(x => x.Token == hash);
        if (u.Activo != true || t.Usado != false || t.FechaExpiracion <= DateTime.UtcNow)
            return BadRequest(new { mensaje = "El enlace no es válido o ya venció." });

        u.ContrasenaHash = Seguridad.Hashear(u, input.Contrasena);
        u.RequiereCambio = false;
        u.IntentosFallidos = 0;
        u.BloqueadoHasta = null;
        await Seguridad.InvalidarTokens(db, u.IdUsuario);
        await Seguridad.CerrarSesiones(db, u.IdUsuario);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    async Task Registrar(Usuario? u, string resultado, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)
    {
        db.LogAcceso.Add(new LogAcceso { IdUsuario = u?.IdUsuario, Fecha = DateTime.UtcNow, Resultado = resultado, Ip = HttpContext.Connection.RemoteIpAddress?.ToString() });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
