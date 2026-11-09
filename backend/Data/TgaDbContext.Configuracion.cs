using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Models;

namespace PlataformaTGA.Data;

// Lo que el scaffolding no trae: llaves naturales, CHECK y catálogos.
// TgaDbContext.cs queda como lo generó EF; los cambios al modelo van aquí o en las entidades.
public partial class TgaDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Las asigna la universidad; sin esto EF las crea AUTO_INCREMENT.
        modelBuilder.Entity<Alumno>().Property(e => e.ClaveAlumno).ValueGeneratedNever();
        modelBuilder.Entity<Egresado>().Property(e => e.ClaveAlumno).ValueGeneratedNever();

        // El scaffolding omite DEFAULT 0 en los bool NOT NULL porque coincide con el default de C#.
        modelBuilder.Entity<Alumno>().Property(e => e.PendienteConciliar).HasDefaultValue(false);
        modelBuilder.Entity<Documento>().Property(e => e.Verificado).HasDefaultValue(false);
        modelBuilder.Entity<ProfesorDetalle>().Property(e => e.Externo).HasDefaultValue(false);

        modelBuilder.Entity<Tesis>().ToTable(t => t.HasCheckConstraint("ck_tesis_solicitud",
            "estado_solicitud IN ('Pendiente', 'Aceptada', 'Rechazada')"));
        modelBuilder.Entity<Revision>().ToTable(t =>
        {
            t.HasCheckConstraint("ck_revision_estado",
                "estado IN ('Abierta', 'En consolidación', 'En firma', 'Cerrada')");
            t.HasCheckConstraint("ck_revision_calif", Calif0a100);
        });
        modelBuilder.Entity<RevisionProfesor>().ToTable(t => t.HasCheckConstraint("ck_revprof_calif", Calif0a100));
        modelBuilder.Entity<Empleo>().ToTable(t => t.HasCheckConstraint("ck_empleo_tipo", "tipo IN ('Empleo', 'Beca', 'Otro')"));

        SembrarCatalogos(modelBuilder);
    }

    const string Calif0a100 =
        "calif_documento <= 100 AND calif_exposicion <= 100 AND calif_dominio <= 100 AND calif_grado_avance <= 100";

    static void SembrarCatalogos(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatProgramaAcademico>().HasData(
            new CatProgramaAcademico { IdPrograma = 1, Nombre = "Maestría en Tecnología y Gestión del Agua", Descripcion = "Programa de maestría de la Facultad de Ingeniería de la UASLP", Activo = true });

        // El profesor externo usa el rol Profesor y se distingue por profesor_detalle.externo.
        modelBuilder.Entity<CatRolUsuario>().HasData(
            new CatRolUsuario { IdRolUsuario = 1, Nombre = "Coordinador", Descripcion = "Gestiona alumnos, titulación, comités, egresados, reportes y usuarios; también es profesor", Activo = true },
            new CatRolUsuario { IdRolUsuario = 2, Nombre = "Administrador", Descripcion = "Lo mismo que el coordinador, sin la parte de profesor, más el registro de accesos", Activo = true },
            new CatRolUsuario { IdRolUsuario = 3, Nombre = "Profesor", Descripcion = "Consulta a los alumnos de sus comités, atiende solicitudes de dirección y evalúa", Activo = true },
            new CatRolUsuario { IdRolUsuario = 4, Nombre = "Alumno", Descripcion = "Consulta su titulación, su comité y sus evaluaciones; sube evidencias y su TOEFL", Activo = true },
            new CatRolUsuario { IdRolUsuario = 5, Nombre = "Egresado", Descripcion = "Actualiza su perfil, su historial laboral y sus constancias", Activo = true });

        modelBuilder.Entity<CatModalidadTitulacion>().HasData(
            new CatModalidadTitulacion { IdModalidad = 1, Nombre = "Tesis", Descripcion = "Titulación mediante trabajo de tesis", Activo = true },
            new CatModalidadTitulacion { IdModalidad = 2, Nombre = "Artículo científico", Descripcion = "Titulación mediante publicación de artículo científico", Activo = true });

        modelBuilder.Entity<CatEstadoAcademico>().HasData(
            new CatEstadoAcademico { IdEstadoAcademico = 1, Nombre = "Activo", Descripcion = "Alumno inscrito y cursando el programa" },
            new CatEstadoAcademico { IdEstadoAcademico = 2, Nombre = "Titulado", Descripcion = "Alumno que concluyó su titulación" },
            new CatEstadoAcademico { IdEstadoAcademico = 3, Nombre = "Baja definitiva", Descripcion = "Alumno separado del programa de forma permanente" },
            new CatEstadoAcademico { IdEstadoAcademico = 4, Nombre = "Baja temporal", Descripcion = "Alumno con suspensión temporal; puede reingresar" });

        modelBuilder.Entity<CatEstadoTesis>().HasData(
            new CatEstadoTesis { IdEstadoTesis = 1, Nombre = "Sin iniciar", Descripcion = "Trabajo registrado sin avance" },
            new CatEstadoTesis { IdEstadoTesis = 2, Nombre = "En progreso", Descripcion = "Trabajo en desarrollo" },
            new CatEstadoTesis { IdEstadoTesis = 3, Nombre = "En evaluación", Descripcion = "Trabajo en evaluación por el comité tutorial" },
            new CatEstadoTesis { IdEstadoTesis = 4, Nombre = "Aceptada", Descripcion = "Trabajo con resolución final favorable" },
            new CatEstadoTesis { IdEstadoTesis = 5, Nombre = "Cancelada", Descripcion = "Trabajo cerrado por baja definitiva del alumno" });

        modelBuilder.Entity<CatEstadoToefl>().HasData(
            new CatEstadoToefl { IdEstadoToefl = 1, Nombre = "Pendiente", Descripcion = "El alumno no ha presentado el examen" },
            new CatEstadoToefl { IdEstadoToefl = 2, Nombre = "Presentado", Descripcion = "Comprobante enviado, pendiente de aprobar" },
            new CatEstadoToefl { IdEstadoToefl = 3, Nombre = "Aprobado", Descripcion = "Requisito TOEFL cumplido" });

        modelBuilder.Entity<CatRolComite>().HasData(
            new CatRolComite { IdRolComite = 1, Nombre = "Director", Descripcion = "Dirige el trabajo y arma el comité" },
            new CatRolComite { IdRolComite = 2, Nombre = "Codirector", Descripcion = "Comparte la dirección del trabajo (opcional)" },
            new CatRolComite { IdRolComite = 3, Nombre = "Sinodal", Descripcion = "Integrante del comité tutorial" });

        modelBuilder.Entity<CatTipoDocumento>().HasData(
            new CatTipoDocumento { IdTipoDocumento = 1, Nombre = "Avance", Descripcion = "Documento de avance que sube el alumno" },
            new CatTipoDocumento { IdTipoDocumento = 2, Nombre = "Evidencia", Descripcion = "Evidencia que sube el alumno" },
            new CatTipoDocumento { IdTipoDocumento = 3, Nombre = "Acta de evaluación", Descripcion = "Acta que genera el sistema al cerrar la evaluación" },
            new CatTipoDocumento { IdTipoDocumento = 4, Nombre = "Formato oficial", Descripcion = "Formato institucional para descarga" },
            new CatTipoDocumento { IdTipoDocumento = 5, Nombre = "Trabajo final", Descripcion = "Versión final del trabajo de titulación" },
            new CatTipoDocumento { IdTipoDocumento = 6, Nombre = "Reporte", Descripcion = "Reporte generado por el sistema" },
            new CatTipoDocumento { IdTipoDocumento = 7, Nombre = "Constancia", Descripcion = "Constancia que sube el egresado" },
            new CatTipoDocumento { IdTipoDocumento = 8, Nombre = "Comprobante TOEFL", Descripcion = "Comprobante del examen TOEFL que sube el alumno" });

        modelBuilder.Entity<CatEstadoEgresado>().HasData(
            new CatEstadoEgresado { IdEstadoEgresado = 1, Nombre = "Localizable", Descripcion = "Egresado con datos de contacto vigentes" },
            new CatEstadoEgresado { IdEstadoEgresado = 2, Nombre = "No localizable", Descripcion = "Sin respuesta tras tres intentos de contacto" });

        modelBuilder.Entity<CatSituacionLaboral>().HasData(
            new CatSituacionLaboral { IdSituacionLaboral = 1, Nombre = "Con trabajo", Descripcion = "Tiene un empleo vigente" },
            new CatSituacionLaboral { IdSituacionLaboral = 2, Nombre = "Sin trabajo", Descripcion = "No tiene ningún empleo vigente" },
            new CatSituacionLaboral { IdSituacionLaboral = 3, Nombre = "Becario", Descripcion = "Tiene una beca vigente" },
            new CatSituacionLaboral { IdSituacionLaboral = 4, Nombre = "Otro", Descripcion = "Tiene una actividad vigente de otro tipo" });

        modelBuilder.Entity<CatTipoReporte>().HasData(
            new CatTipoReporte { IdTipoReporte = 1, Nombre = "Reporte de egresados", Descripcion = "Egresados por generación, estado y situación laboral" },
            new CatTipoReporte { IdTipoReporte = 2, Nombre = "Reporte de avance de titulación", Descripcion = "Avance de titulación con rezago por avance y por tiempo" },
            new CatTipoReporte { IdTipoReporte = 3, Nombre = "Reporte de situación laboral", Descripcion = "Tiempo para emplearse y relación del empleo con la maestría" },
            new CatTipoReporte { IdTipoReporte = 4, Nombre = "Reporte por generación", Descripcion = "Estado de los alumnos y titulación en tiempo por generación" });

        modelBuilder.Entity<CatFormatoReporte>().HasData(
            new CatFormatoReporte { IdFormatoReporte = 1, Nombre = "PDF" },
            new CatFormatoReporte { IdFormatoReporte = 2, Nombre = "Excel" });
    }
}
