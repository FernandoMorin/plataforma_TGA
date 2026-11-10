using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Controllers;

public record BusquedaRegistroInput([MaxLength(UniversidadExterna.MaxResultados)] int[] Claves, [MaxLength(100)] string? Nombre);
public record ImportacionInput(
    [Required, MinLength(1), MaxLength(UniversidadExterna.MaxResultados)] int[] Claves,
    int IdGeneracion,
    int IdPeriodo,
    DateTime FechaIngreso,
    [Range(1, 8)] int Semestre);

// Importar alumnos desde el registro de la universidad (Figma I1-I3). Los alumnos no se dan de alta a mano.
[ApiController, Route("api/alumnos/importacion"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public class ImportacionController(TgaDbContext db, UniversidadExterna universidad) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };
    ObjectResult SinRegistro() => StatusCode(503, Error("El registro de la universidad no está disponible."));

    // I1: busca en el registro y dice, por cada estudiante, si se puede importar.
    [HttpPost("buscar")]
    public async Task<IActionResult> Buscar(BusquedaRegistroInput input)
    {
        if (!universidad.Configurado) return SinRegistro();
        List<EstudianteUniversidad> encontrados;
        try { encontrados = await universidad.Buscar(input.Claves ?? [], input.Nombre); }
        catch (MySqlException) { return SinRegistro(); }

        var claves = encontrados.Select(e => e.Clave).ToList();
        var correos = encontrados.Select(e => e.Correo.ToLowerInvariant()).ToList();
        var yaEstan = await db.Alumno.Where(a => claves.Contains(a.ClaveAlumno)).Select(a => a.ClaveAlumno).ToListAsync();
        var correosOcupados = await db.Usuario.Where(u => correos.Contains(u.Correo)).Select(u => u.Correo).ToListAsync();

        return Ok(new
        {
            resultados = encontrados.OrderBy(e => e.Clave).Select(e => new
            {
                clave = e.Clave,
                nombre = Nombre(e),
                correo = e.Correo.ToLowerInvariant(),
                estatus = e.Estatus,
                situacion = Situacion(e, yaEstan, correosOcupados),
            }),
            noEncontradas = (input.Claves ?? []).Distinct().Except(claves).Order(),
        });
    }

    // I2-I3: importa a los seleccionados con los datos de ingreso del grupo. Cada alumno en su propia
    // transacción: si uno falla, los demás se importan. Devuelve las contraseñas iniciales, que solo
    // existen en esta respuesta (en la base queda el hash).
    [HttpPost]
    public async Task<IActionResult> Importar(ImportacionInput input)
    {
        if (!universidad.Configurado) return SinRegistro();
        if (!await db.Generacion.AnyAsync(g => g.IdGeneracion == input.IdGeneracion)) return BadRequest(Error("La generación no existe."));
        if (!await db.PeriodoAcademico.AnyAsync(p => p.IdPeriodo == input.IdPeriodo)) return BadRequest(Error("El periodo no existe."));
        var rolAlumno = await db.CatRolUsuario.Where(r => r.Nombre == Roles.Alumno).Select(r => r.IdRolUsuario).SingleAsync();
        var programa = await db.CatProgramaAcademico.OrderBy(p => p.IdPrograma).Select(p => p.IdPrograma).FirstAsync();

        // Los datos se vuelven a leer del registro: no se confía en lo que mande el navegador.
        List<EstudianteUniversidad> registro;
        try { registro = await universidad.Buscar(input.Claves, null); }
        catch (MySqlException) { return SinRegistro(); }

        var importados = new List<object>();
        var errores = new List<object>();
        foreach (var clave in input.Claves.Distinct())
        {
            var e = registro.FirstOrDefault(x => x.Clave == clave);
            var correo = e?.Correo.ToLowerInvariant();
            var motivo = e is null ? "No está en el registro de la universidad."
                : Situacion(e, await db.Alumno.Where(a => a.ClaveAlumno == clave).Select(a => a.ClaveAlumno).ToListAsync(),
                               await db.Usuario.Where(u => u.Correo == correo).Select(u => u.Correo).ToListAsync()) switch
                {
                    "baja" => "Está dado de baja en la universidad.",
                    "ya-esta" => "Ya está en el sistema.",
                    "correo-en-uso" => "Su correo ya tiene una cuenta en el sistema.",
                    _ => null,
                };
            if (motivo is not null) { errores.Add(new { clave, mensaje = motivo }); continue; }

            var contrasena = Seguridad.ContrasenaInicial();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var usuario = new Usuario
                {
                    Correo = correo!, IdRolUsuario = rolAlumno, Activo = true, RequiereCambio = true, IntentosFallidos = 0,
                    ClaveInstitucional = clave.ToString(), Nombre = Nombre(e!), FechaCreacion = DateTime.UtcNow,
                };
                usuario.ContrasenaHash = Seguridad.Hashear(usuario, contrasena);
                db.Alumno.Add(new Alumno
                {
                    ClaveAlumno = clave, Nombre = e!.Nombre, ApellidoPaterno = e.ApellidoPaterno, ApellidoMaterno = e.ApellidoMaterno,
                    CorreoInstitucional = correo, FechaIngreso = input.FechaIngreso.Date, IdUsuarioNavigation = usuario,
                    IdPrograma = programa, IdGeneracion = input.IdGeneracion, IdEstadoAcademico = EstadoAcademico.Activo,
                    AlumnoDetalle = [new AlumnoDetalle { IdPeriodo = input.IdPeriodo, Semestre = input.Semestre, FechaRegistro = DateTime.Today }],
                });
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                importados.Add(new { clave, nombre = Nombre(e), correo, contrasena });
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                errores.Add(new { clave, mensaje = "No se pudo importar." });
            }
            finally
            {
                // Sin esto, las entidades de un alumno fallido se quedan pendientes y arrastran a los siguientes.
                db.ChangeTracker.Clear();
            }
        }
        return Ok(new { importados, errores });
    }

    static string Nombre(EstudianteUniversidad e) =>
        string.Join(' ', new[] { e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno }.Where(s => !string.IsNullOrWhiteSpace(s)));

    static string Situacion(EstudianteUniversidad e, List<int> yaEstan, List<string> correosOcupados) =>
        yaEstan.Contains(e.Clave) ? "ya-esta"
        : e.Estatus == "Baja" ? "baja"
        : correosOcupados.Contains(e.Correo.ToLowerInvariant()) ? "correo-en-uso"
        : "listo";
}
