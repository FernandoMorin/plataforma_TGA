using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Auth;

// Nombres de CAT_rol_usuario.
public static class Roles
{
    public const string Coordinador = "Coordinador", Administrador = "Administrador", Profesor = "Profesor",
        Alumno = "Alumno", Egresado = "Egresado";
}

public static class Seguridad
{
    public const int Intentos = 5, BloqueoMinutos = 15, InactividadMinutos = 15, SesionMaximaHoras = 8;
    public const string ClaimSesion = "sid";
    // Primer acceso (P1): la sesión con contraseña temporal solo puede cambiarla o salir. Al cambiarla se
    // cierran todas las sesiones, así que la marca nunca queda vieja en la cookie.
    public const string ClaimCambio = "cambio";
    public static bool PermitidoSinCambio(PathString ruta) =>
        ruta.StartsWithSegments("/api/auth") || ruta.StartsWithSegments("/api/cuenta/contrasena") || ruta.StartsWithSegments("/api/salud");

    public static bool Verificar(Usuario u, string contrasena)
    {
        try { return new PasswordHasher<Usuario>().VerifyHashedPassword(u, u.ContrasenaHash, contrasena) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
    }

    public static string Hashear(Usuario u, string contrasena) => new PasswordHasher<Usuario>().HashPassword(u, contrasena);

    // Regla de contraseña (la misma del sistema anterior): de 12 a 128 caracteres, con letras y números.
    public static bool ContrasenaValida(string? p) => p is { Length: >= 12 and <= 128 } && p.Any(char.IsLetter) && p.Any(char.IsDigit);

    // Sin caracteres ambiguos (O/0, l/1/I): estas contraseñas se dictan o se copian a mano.
    const string AlfabetoInicial = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    // Contraseña inicial de una cuenta nueva: 16 caracteres en grupos de cuatro (AbC2-...).
    // Se valida con la misma regla, así nunca genera una que el sistema rechace. Se muestra una sola vez.
    public static string ContrasenaInicial()
    {
        while (true)
        {
            var c = RandomNumberGenerator.GetString(AlfabetoInicial, 16);
            var p = string.Join('-', c[..4], c[4..8], c[8..12], c[12..]);
            if (ContrasenaValida(p)) return p;
        }
    }

    public static Task CerrarSesiones(TgaDbContext db, int idUsuario) =>
        db.SesionUsuario.Where(s => s.UsuarioId == idUsuario).ExecuteDeleteAsync();

    // Tokens de recuperación: se entrega el token y en la base solo queda su hash.
    public static string Token() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    public static Task InvalidarTokens(TgaDbContext db, int idUsuario) =>
        db.TokenRecuperacion.Where(t => t.IdUsuario == idUsuario).ExecuteUpdateAsync(s => s.SetProperty(t => t.Usado, true));

    // El profesor externo deja de tener acceso al pasar su vigencia; la cuenta queda registrada.
    public static Task<bool> ExternoVencido(TgaDbContext db, int idUsuario) =>
        db.ProfesorDetalle.AnyAsync(p => p.IdUsuario == idUsuario && p.Externo && p.VigenteHasta < DateTime.Today);

    public static int IdUsuario(this ClaimsPrincipal p) => int.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// Cada petición autenticada revisa que la sesión siga viva en la base: así cerrar sesión,
// bloquear o desactivar una cuenta surte efecto de inmediato, aunque la cookie siga vigente.
public class ValidarSesion(TgaDbContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var sid = context.Principal?.FindFirstValue(Seguridad.ClaimSesion);
        var ahora = DateTime.UtcNow;
        var vigente = await db.SesionUsuario.AsNoTracking().AnyAsync(s =>
            s.Id == sid &&
            s.UltimaActividad > ahora.AddMinutes(-Seguridad.InactividadMinutos) &&
            s.Creada > ahora.AddHours(-Seguridad.SesionMaximaHoras) &&
            s.Usuario.Activo == true && !(s.Usuario.BloqueadoHasta > ahora) &&
            !s.Usuario.ProfesorDetalle.Any(p => p.Externo && p.VigenteHasta < DateTime.Today));
        if (vigente) return;

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync();
        if (sid != null) await db.SesionUsuario.Where(s => s.Id == sid).ExecuteDeleteAsync();
    }

    // Es una API: sin redirecciones a páginas de login.
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> c) { c.Response.StatusCode = 401; return Task.CompletedTask; }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> c) { c.Response.StatusCode = 403; return Task.CompletedTask; }
}
