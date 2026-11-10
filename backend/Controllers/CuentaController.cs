using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

public record ContactoInput([Required, EmailAddress, MaxLength(100)] string CorreoPersonal, [Required, MaxLength(20)] string Telefono);
public record ContrasenaInput([Required, MaxLength(128)] string Actual, [Required, MaxLength(128)] string Nueva);

// La cuenta de quien inició sesión: primer acceso (P1), completar datos (P3) y Mi cuenta (P4, MC1-MC4).
[ApiController, Route("api/cuenta"), Authorize]
public class CuentaController(TgaDbContext db) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    [HttpGet]
    public async Task<IActionResult> Datos()
    {
        var id = User.IdUsuario();
        var u = await db.Usuario.AsNoTracking().Where(x => x.IdUsuario == id)
            .Select(x => new { x.Nombre, x.Correo, x.ClaveInstitucional }).SingleAsync();
        var alumno = await db.Alumno.AsNoTracking().Where(a => a.IdUsuario == id).Select(a => new
        {
            clave = a.ClaveAlumno, correoInstitucional = a.CorreoInstitucional, correoPersonal = a.CorreoPersonal,
            telefono = a.Telefono, generacion = a.IdGeneracionNavigation!.NombreGeneracion,
            identificadorEgresado = a.Egresado!.IdentificadorEgresado,
        }).FirstOrDefaultAsync();
        return Ok(new { u.Nombre, u.Correo, u.ClaveInstitucional, rol = User.FindFirstValue(ClaimTypes.Role), alumno });
    }

    // Datos de contacto del alumno: los completa en su segundo acceso (P3) y los edita en Mi cuenta (P4).
    [HttpPut("contacto"), Authorize(Roles = Roles.Alumno)]
    public async Task<IActionResult> Contacto(ContactoInput input)
    {
        var a = await db.Alumno.SingleOrDefaultAsync(x => x.IdUsuario == User.IdUsuario());
        if (a is null) return NotFound();
        a.CorreoPersonal = input.CorreoPersonal.Trim().ToLowerInvariant();
        a.Telefono = input.Telefono.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Cambio de contraseña (P1 y Mi cuenta). Cierra todas las sesiones de la cuenta, también esta:
    // el usuario vuelve a entrar con la nueva.
    [HttpPost("contrasena"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Contrasena(ContrasenaInput input)
    {
        if (!Seguridad.ContrasenaValida(input.Nueva)) return BadRequest(Error("Usa 12 caracteres o más, con letras y números."));
        if (input.Nueva == input.Actual) return BadRequest(Error("La nueva contraseña debe ser distinta a la actual."));

        var id = User.IdUsuario();
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = (await db.Usuario.FromSqlInterpolated($"SELECT * FROM usuario WHERE id_usuario = {id} FOR UPDATE").ToListAsync()).Single();
        if (!Seguridad.Verificar(u, input.Actual)) return BadRequest(Error("La contraseña actual no es correcta."));

        u.ContrasenaHash = Seguridad.Hashear(u, input.Nueva);
        u.RequiereCambio = false;
        await Seguridad.CerrarSesiones(db, id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}
