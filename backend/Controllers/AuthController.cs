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

public record LoginInput(
    [Required, EmailAddress, MaxLength(100)] string Correo,
    [Required, MaxLength(128)] string Contrasena,
    bool PortalEgresados = false);

[ApiController, Route("api/auth")]
public class AuthController(TgaDbContext db) : ControllerBase
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

        if (u is null || u.Activo != true)
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
        ];
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = ahora.AddHours(Seguridad.SesionMaximaHoras) });
        return Ok(new { rol, requiereCambio = u.RequiereCambio == true });
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var u = await db.Usuario.AsNoTracking().SingleAsync(x => x.IdUsuario == User.IdUsuario());
        return Ok(new
        {
            u.Nombre, u.Correo, u.ClaveInstitucional,
            rol = User.FindFirstValue(ClaimTypes.Role),
            requiereCambio = u.RequiereCambio == true,
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

    async Task Registrar(Usuario? u, string resultado, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)
    {
        db.LogAcceso.Add(new LogAcceso { IdUsuario = u?.IdUsuario, Fecha = DateTime.UtcNow, Resultado = resultado, Ip = HttpContext.Connection.RemoteIpAddress?.ToString() });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
