using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Models;

namespace PlataformaTGA.Data;

// Datos de prueba: solo en Development y solo si la base no tiene usuarios.
// Los catálogos no van aquí: vienen en las migraciones.
public static class SeederDesarrollo
{
    public const string Contrasena = "Prueba2026!";

    const int Coordinador = 1, Administrador = 2, Profesor = 3, AlumnoRol = 4, EgresadoRol = 5;
    const int Programa = 1, EstadoActivo = 1, EstadoTitulado = 2, Localizable = 1;

    public static async Task SembrarAsync(TgaDbContext db)
    {
        if (await db.Usuario.AnyAsync()) return;

        var hash = new PasswordHasher<Usuario>().HashPassword(null!, Contrasena);
        Usuario U(string correo, int rol, string nombre, string? clave) => new()
        {
            Correo = correo, ContrasenaHash = hash, IdRolUsuario = rol, Nombre = nombre,
            ClaveInstitucional = clave, Activo = true, RequiereCambio = false, IntentosFallidos = 0,
        };

        var g2022 = new Generacion { IdPrograma = Programa, AnioIngreso = 2022, AnioEgresoEstimado = 2024, NombreGeneracion = "2022-2024" };
        var g2025 = new Generacion { IdPrograma = Programa, AnioIngreso = 2025, AnioEgresoEstimado = 2027, NombreGeneracion = "2025-2027" };
        var g2026 = new Generacion { IdPrograma = Programa, AnioIngreso = 2026, AnioEgresoEstimado = 2028, NombreGeneracion = "2026-2028" };
        var otono2026 = new PeriodoAcademico { Nombre = "2026-2027 Otoño", FechaInicio = new(2026, 8, 1), FechaFin = new(2026, 12, 15), TipoPeriodo = "Semestral" };
        db.AddRange(g2022, g2025, g2026, otono2026);

        db.Usuario.Add(U("admin@uaslp.mx", Administrador, "Administrador del sistema", null));

        void Prof(string rpe, string nombre, string paterno, string materno, string correo, int rol,
                  bool externo = false, string? institucion = null, DateTime? vigenteHasta = null)
        {
            db.ProfesorDetalle.Add(new ProfesorDetalle
            {
                RpeNavigation = new Profesor { Rpe = rpe, Nombre = nombre, ApellidoPaterno = paterno, ApellidoMaterno = materno },
                IdUsuarioNavigation = U(correo, rol, $"{nombre} {paterno} {materno}", rpe),
                CorreoInstitucional = correo, Activo = true,
                Externo = externo, Institucion = institucion, VigenteHasta = vigenteHasta,
            });
        }
        Prof("100202", "Carlos", "Rivera", "Morales", "crimo@uaslp.mx", Coordinador);
        Prof("100201", "Ana", "Torres", "Ramírez", "atora@uaslp.mx", Profesor);
        Prof("100203", "Laura", "Gómez", "Fernández", "lgofe@uaslp.mx", Profesor);
        Prof("EXT-001", "Miguel", "Ortiz", "Luna", "mortiz@uaq.mx", Profesor,
             externo: true, institucion: "Universidad Autónoma de Querétaro", vigenteHasta: new(2027, 7, 31));

        Alumno Al(int clave, string nombre, string paterno, string materno, Generacion gen, int estado, Usuario? usuario) => new()
        {
            ClaveAlumno = clave, Nombre = nombre, ApellidoPaterno = paterno, ApellidoMaterno = materno,
            CorreoInstitucional = $"a{clave}@alumnos.uaslp.mx", IdPrograma = Programa, IdGeneracionNavigation = gen,
            FechaIngreso = new(gen.AnioIngreso!.Value, 8, 1), IdEstadoAcademico = estado, IdUsuarioNavigation = usuario,
        };

        foreach (var (clave, nombre, paterno, materno, gen, semestre) in new[]
        {
            (300101, "José", "Hernández", "Castillo", g2025, 3),
            (300102, "Mariana", "López", "Ruiz", g2026, 1),
        })
        {
            var alumno = Al(clave, nombre, paterno, materno, gen, EstadoActivo,
                U($"a{clave}@alumnos.uaslp.mx", AlumnoRol, $"{nombre} {paterno} {materno}", clave.ToString()));
            db.AlumnoDetalle.Add(new AlumnoDetalle { ClaveAlumnoNavigation = alumno, IdPeriodoNavigation = otono2026, Semestre = semestre, FechaRegistro = new(2026, 8, 1) });
        }

        // El egresado entra con su correo personal; su clave institucional es el identificador de egresado.
        db.Egresado.Add(new Egresado
        {
            ClaveAlumnoNavigation = Al(258001, "Rosa", "Sánchez", "Cruz", g2022, EstadoTitulado,
                U("rosa.sanchez@gmail.com", EgresadoRol, "Rosa Sánchez Cruz", "2022RSC01")),
            IdentificadorEgresado = "2022RSC01", FechaEgreso = new(2024, 7, 15), IdEstadoEgresado = Localizable,
            Correo = "rosa.sanchez@gmail.com", UltimaActualizacion = new(2026, 6, 10),
        });

        await db.SaveChangesAsync();
    }
}
