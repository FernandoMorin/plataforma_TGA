using Microsoft.AspNetCore.Identity;
using PlataformaTGA.Data;
using PlataformaTGA.Models;

namespace PlataformaTGA.Pruebas.Infraestructura;

public record ProfesorPrueba(string Rpe, string Correo);
public record AlumnoPrueba(int Clave, string Correo, int IdGeneracion);
public record TrabajoPrueba(int IdTesis, AlumnoPrueba Alumno, ProfesorPrueba Director, ProfesorPrueba[] Sinodales)
{
    public ProfesorPrueba[] Integrantes => [Director, .. Sinodales];
}

// Preparación de cada caso, directo en la base (como el seeder): cada prueba crea sus propios datos
// con claves únicas, así no depende de lo que dejaron las demás.
public static class Datos
{
    public const string Contrasena = "Pruebas2026tga";
    const int Programa = 1, RolCoordinador = 1, RolProfesor = 3, RolAlumno = 4;

    static int consecutivo = 900000;
    static int Siguiente() => Interlocked.Increment(ref consecutivo);

    static Usuario NuevoUsuario(string correo, int rol, string nombre, string? clave) => new()
    {
        Correo = correo, ContrasenaHash = new PasswordHasher<Usuario>().HashPassword(null!, Contrasena), IdRolUsuario = rol,
        Nombre = nombre, ClaveInstitucional = clave, Activo = true, RequiereCambio = false, IntentosFallidos = 0,
    };

    public static Task<int> PeriodoAsync(Aplicacion app, DateTime inicio, DateTime fin, string? nombre = null) =>
        app.ConBaseAsync(async db =>
        {
            var p = new PeriodoAcademico { Nombre = nombre ?? $"Periodo {inicio:yyyy-MM}", FechaInicio = inicio, FechaFin = fin, TipoPeriodo = "Semestral" };
            db.PeriodoAcademico.Add(p);
            await db.SaveChangesAsync();
            return p.IdPeriodo;
        });

    // Periodos ya terminados para los informes cerrados de un caso (el más antiguo primero).
    public static async Task<int[]> PeriodosPasadosAsync(Aplicacion app, int cuantos)
    {
        var ids = new int[cuantos];
        for (var i = 0; i < cuantos; i++)
        {
            // Semestres consecutivos que terminan antes de que empiece el periodo en curso.
            var inicio = DateTime.Today.AddMonths(-6 * (cuantos - i) - 2);
            ids[i] = await PeriodoAsync(app, inicio, inicio.AddMonths(5), $"Pasado {Siguiente()}");
        }
        return ids;
    }

    public static Task<int> GeneracionAsync(Aplicacion app) =>
        app.ConBaseAsync(async db =>
        {
            var n = Siguiente();
            var g = new Generacion { IdPrograma = Programa, AnioIngreso = 2025, AnioEgresoEstimado = 2027, NombreGeneracion = $"G-{n}" };
            db.Generacion.Add(g);
            await db.SaveChangesAsync();
            return g.IdGeneracion;
        });

    public static Task<ProfesorPrueba> ProfesorAsync(Aplicacion app, bool coordinador = false) =>
        app.ConBaseAsync(async db =>
        {
            var n = Siguiente();
            var rpe = $"P{n}";
            var correo = $"prof{n}@pruebas.mx";
            db.ProfesorDetalle.Add(new ProfesorDetalle
            {
                RpeNavigation = new Profesor { Rpe = rpe, Nombre = "Profesor", ApellidoPaterno = $"Prueba{n}", ApellidoMaterno = "Uno" },
                IdUsuarioNavigation = NuevoUsuario(correo, coordinador ? RolCoordinador : RolProfesor, $"Profesor Prueba{n}", rpe),
                CorreoInstitucional = correo, Activo = true,
            });
            await db.SaveChangesAsync();
            return new ProfesorPrueba(rpe, correo);
        });

    // Alumno activo inscrito en el periodo en curso con el semestre dado.
    public static Task<AlumnoPrueba> AlumnoAsync(Aplicacion app, int idGeneracion, int semestre, int estado = EstadoAcademico.Activo) =>
        app.ConBaseAsync(async db =>
        {
            var clave = Siguiente();
            var correo = $"a{clave}@pruebas.mx";
            db.AlumnoDetalle.Add(new AlumnoDetalle
            {
                ClaveAlumnoNavigation = new Alumno
                {
                    ClaveAlumno = clave, Nombre = "Alumno", ApellidoPaterno = $"Prueba{clave}", ApellidoMaterno = "Uno",
                    CorreoInstitucional = correo, IdPrograma = Programa, IdGeneracion = idGeneracion, FechaIngreso = new(2025, 8, 1),
                    IdEstadoAcademico = estado, IdModalidad = 1,
                    IdUsuarioNavigation = NuevoUsuario(correo, RolAlumno, $"Alumno Prueba{clave}", clave.ToString()),
                },
                IdPeriodo = app.PeriodoActual, Semestre = semestre, FechaRegistro = DateTime.Today,
            });
            await db.SaveChangesAsync();
            return new AlumnoPrueba(clave, correo, idGeneracion);
        });

    // Trabajo aceptado con comité completo: director y dos sinodales (lo mínimo para abrir informes).
    public static async Task<TrabajoPrueba> TrabajoAsync(Aplicacion app, int semestre = 2, int? idGeneracion = null)
    {
        var alumno = await AlumnoAsync(app, idGeneracion ?? await GeneracionAsync(app), semestre);
        var director = await ProfesorAsync(app);
        ProfesorPrueba[] sinodales = [await ProfesorAsync(app), await ProfesorAsync(app)];
        var idTesis = await app.ConBaseAsync(async db =>
        {
            var t = new Tesis
            {
                Titulo = $"Trabajo de prueba {alumno.Clave}", ClaveAlumno = alumno.Clave, IdPrograma = Programa, IdModalidad = 1,
                EstadoSolicitud = "Aceptada", RpeDirector = director.Rpe, IdEstadoTesis = EstadoTesis.SinIniciar, PorcentajeActual = 0,
                FechaSolicitud = DateTime.Today, FechaResolucion = DateTime.Today, FechaRegistro = DateTime.Today,
            };
            var comite = new Comite { IdTesisNavigation = t, Clave = $"CT-PRB-{alumno.Clave}" };
            comite.ComiteProfesor.Add(new ComiteProfesor { Rpe = director.Rpe, IdRolComite = RolComite.Director, Activo = true, FechaAsignacion = DateTime.Today });
            foreach (var s in sinodales)
                comite.ComiteProfesor.Add(new ComiteProfesor { Rpe = s.Rpe, IdRolComite = RolComite.Sinodal, Activo = true, FechaAsignacion = DateTime.Today });
            db.Comite.Add(comite);
            await db.SaveChangesAsync();
            return t.IdTesis;
        });
        return new TrabajoPrueba(idTesis, alumno, director, sinodales);
    }

    // Informe ya cerrado en un periodo pasado, con la revisión y la firma de cada integrante.
    // Deja el avance de la tesis igual al del informe, como lo hace el cierre real.
    public static Task<int> InformeCerradoAsync(Aplicacion app, TrabajoPrueba t, int idPeriodo, decimal avance, params string[] compromisos) =>
        app.ConBaseAsync(async db =>
        {
            var fecha = (await db.PeriodoAcademico.FindAsync(idPeriodo))!.FechaFin!.Value;
            var r = new Revision
            {
                IdTesis = t.IdTesis, IdPeriodo = idPeriodo, Estado = "Cerrada", Fecha = fecha, PorcentajeAvance = avance,
                Comentarios = $"Informe con {avance} %", CalifDocumento = 80, CalifExposicion = 80, CalifDominio = 80, CalifGradoAvance = 80,
            };
            foreach (var p in t.Integrantes)
                r.RevisionProfesor.Add(new RevisionProfesor
                {
                    Rpe = p.Rpe, Comentarios = "Revisión", CalifDocumento = 80, CalifExposicion = 80, CalifDominio = 80, CalifGradoAvance = 80,
                    PorcentajeAvance = avance, FechaEnvio = fecha, FechaConformidad = fecha,
                });
            foreach (var c in compromisos)
                r.AcuerdoEvaluacion.Add(new AcuerdoEvaluacion { Descripcion = c, FechaCompromiso = fecha.AddMonths(3), Cumplido = false });
            db.Revision.Add(r);
            var tesis = await db.Tesis.FindAsync(t.IdTesis);
            tesis!.PorcentajeActual = avance;
            tesis.IdEstadoTesis = EstadoTesis.EnProgreso;
            await db.SaveChangesAsync();
            return r.IdRevision;
        });

    // Egresado activo (titulado) de la generación dada.
    public static Task<int> EgresadoAsync(Aplicacion app, int idGeneracion, bool noLocalizable = false) =>
        app.ConBaseAsync(async db =>
        {
            var clave = Siguiente();
            db.Egresado.Add(new Egresado
            {
                ClaveAlumnoNavigation = new Alumno
                {
                    ClaveAlumno = clave, Nombre = "Egresado", ApellidoPaterno = $"Prueba{clave}", ApellidoMaterno = "Uno",
                    IdPrograma = Programa, IdGeneracion = idGeneracion, FechaIngreso = new(2022, 8, 1), IdEstadoAcademico = EstadoAcademico.Titulado,
                },
                IdentificadorEgresado = $"EG{clave}", FechaEgreso = new(2024, 7, 15), Activo = true,
                IdEstadoEgresado = noLocalizable ? Egresados.NoLocalizable : Egresados.Localizable,
                Correo = $"e{clave}@pruebas.mx", UltimaActualizacion = DateTime.Today,
            });
            await db.SaveChangesAsync();
            return clave;
        });

    // Un PDF mínimo válido (la API revisa la cabecera %PDF-) del tamaño pedido.
    public static byte[] Pdf(int bytes)
    {
        var contenido = new byte[bytes];
        "%PDF-1.4\n"u8.CopyTo(contenido);
        return contenido;
    }
}
