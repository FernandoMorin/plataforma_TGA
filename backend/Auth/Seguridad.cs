using System.Security.Claims;
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

    public static bool Verificar(Usuario u, string contrasena)
    {
        try { return new PasswordHasher<Usuario>().VerifyHashedPassword(u, u.ContrasenaHash, contrasena) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
    }

    public static string Hashear(Usuario u, string contrasena) => new PasswordHasher<Usuario>().HashPassword(u, contrasena);

    public static Task CerrarSesiones(TgaDbContext db, int idUsuario) =>
        db.SesionUsuario.Where(s => s.UsuarioId == idUsuario).ExecuteDeleteAsync();

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
            s.Usuario.Activo == true && !(s.Usuario.BloqueadoHasta > ahora));
        if (vigente) return;

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync();
        if (sid != null) await db.SesionUsuario.Where(s => s.Id == sid).ExecuteDeleteAsync();
    }

    // Es una API: sin redirecciones a páginas de login.
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> c) { c.Response.StatusCode = 401; return Task.CompletedTask; }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> c) { c.Response.StatusCode = 403; return Task.CompletedTask; }
}
