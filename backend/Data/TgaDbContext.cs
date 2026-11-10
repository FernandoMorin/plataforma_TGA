using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Models;

namespace PlataformaTGA.Data;

public partial class TgaDbContext : DbContext
{
    public TgaDbContext(DbContextOptions<TgaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActaComite> ActaComite { get; set; }

    public virtual DbSet<AcuerdoEvaluacion> AcuerdoEvaluacion { get; set; }

    public virtual DbSet<Alumno> Alumno { get; set; }

    public virtual DbSet<AlumnoDetalle> AlumnoDetalle { get; set; }

    public virtual DbSet<CatEstadoAcademico> CatEstadoAcademico { get; set; }

    public virtual DbSet<CatEstadoEgresado> CatEstadoEgresado { get; set; }

    public virtual DbSet<CatEstadoTesis> CatEstadoTesis { get; set; }

    public virtual DbSet<CatEstadoToefl> CatEstadoToefl { get; set; }

    public virtual DbSet<CatFormatoReporte> CatFormatoReporte { get; set; }

    public virtual DbSet<CatModalidadTitulacion> CatModalidadTitulacion { get; set; }

    public virtual DbSet<CatProgramaAcademico> CatProgramaAcademico { get; set; }

    public virtual DbSet<CatRolComite> CatRolComite { get; set; }

    public virtual DbSet<CatRolUsuario> CatRolUsuario { get; set; }

    public virtual DbSet<CatSituacionLaboral> CatSituacionLaboral { get; set; }

    public virtual DbSet<CatTipoDocumento> CatTipoDocumento { get; set; }

    public virtual DbSet<CatTipoReporte> CatTipoReporte { get; set; }

    public virtual DbSet<Comite> Comite { get; set; }

    public virtual DbSet<ComiteProfesor> ComiteProfesor { get; set; }

    public virtual DbSet<DetalleEmpleo> DetalleEmpleo { get; set; }

    public virtual DbSet<Documento> Documento { get; set; }

    public virtual DbSet<Egresado> Egresado { get; set; }

    public virtual DbSet<Empleo> Empleo { get; set; }

    public virtual DbSet<Empresa> Empresa { get; set; }

    public virtual DbSet<Generacion> Generacion { get; set; }

    public virtual DbSet<IntentoContacto> IntentoContacto { get; set; }

    public virtual DbSet<LogAcceso> LogAcceso { get; set; }

    public virtual DbSet<PeriodoAcademico> PeriodoAcademico { get; set; }

    public virtual DbSet<Profesor> Profesor { get; set; }

    public virtual DbSet<ProfesorDetalle> ProfesorDetalle { get; set; }

    public virtual DbSet<Reporte> Reporte { get; set; }

    public virtual DbSet<RequisitoToefl> RequisitoToefl { get; set; }

    public virtual DbSet<RetroalimentacionEgresado> RetroalimentacionEgresado { get; set; }

    public virtual DbSet<Revision> Revision { get; set; }

    public virtual DbSet<RevisionProfesor> RevisionProfesor { get; set; }

    public virtual DbSet<SesionUsuario> SesionUsuario { get; set; }

    public virtual DbSet<Tesis> Tesis { get; set; }

    public virtual DbSet<TokenRecuperacion> TokenRecuperacion { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActaComite>(entity =>
        {
            entity.HasKey(e => e.CveActa).HasName("PRIMARY");

            entity.ToTable("acta_comite");

            entity.HasIndex(e => e.ClaveAlumno, "fk_acta_alumno");

            entity.HasIndex(e => e.IdComite, "fk_acta_comite");

            entity.HasIndex(e => e.IdRevision, "fk_acta_revision");

            entity.HasIndex(e => e.IdToefl, "fk_acta_toefl");

            entity.Property(e => e.CveActa)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("cve_acta");
            entity.Property(e => e.ArchivoActa)
                .HasMaxLength(255)
                .HasColumnName("archivo_acta");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'1'")
                .HasColumnName("estado");
            entity.Property(e => e.FechaReunion)
                .HasColumnType("date")
                .HasColumnName("fecha_reunion");
            entity.Property(e => e.IdComite).HasColumnName("id_comite");
            entity.Property(e => e.IdRevision).HasColumnName("id_revision");
            entity.Property(e => e.IdToefl).HasColumnName("id_toefl");
            entity.Property(e => e.NumEvaluacion).HasColumnName("num_evaluacion");
            entity.Property(e => e.Observaciones)
                .HasColumnType("text")
                .HasColumnName("observaciones");
            entity.Property(e => e.Resultado)
                .HasMaxLength(50)
                .HasColumnName("resultado");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.ActaComite)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_acta_alumno");

            entity.HasOne(d => d.IdComiteNavigation).WithMany(p => p.ActaComite)
                .HasForeignKey(d => d.IdComite)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_acta_comite");

            entity.HasOne(d => d.IdRevisionNavigation).WithMany(p => p.ActaComite)
                .HasForeignKey(d => d.IdRevision)
                .HasConstraintName("fk_acta_revision");

            entity.HasOne(d => d.IdToeflNavigation).WithMany(p => p.ActaComite)
                .HasForeignKey(d => d.IdToefl)
                .HasConstraintName("fk_acta_toefl");
        });

        modelBuilder.Entity<AcuerdoEvaluacion>(entity =>
        {
            entity.HasKey(e => e.IdAcuerdo).HasName("PRIMARY");

            entity.ToTable("acuerdo_evaluacion");

            entity.HasIndex(e => e.IdRevision, "fk_acuerdo_revision");

            entity.Property(e => e.IdAcuerdo).HasColumnName("id_acuerdo");
            entity.Property(e => e.Cumplido)
                .HasDefaultValueSql("'0'")
                .HasColumnName("cumplido");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaCompromiso)
                .HasColumnType("date")
                .HasColumnName("fecha_compromiso");
            entity.Property(e => e.IdRevision).HasColumnName("id_revision");

            entity.HasOne(d => d.IdRevisionNavigation).WithMany(p => p.AcuerdoEvaluacion)
                .HasForeignKey(d => d.IdRevision)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_acuerdo_revision");
        });

        modelBuilder.Entity<Alumno>(entity =>
        {
            entity.HasKey(e => e.ClaveAlumno).HasName("PRIMARY");

            entity.ToTable("alumno");

            entity.HasIndex(e => e.IdEstadoAcademico, "fk_alumno_estado_academico");

            entity.HasIndex(e => e.IdGeneracion, "fk_alumno_generacion");

            entity.HasIndex(e => e.IdModalidad, "fk_alumno_modalidad");

            entity.HasIndex(e => e.IdPrograma, "fk_alumno_programa");

            entity.HasIndex(e => e.IdUsuario, "fk_alumno_usuario");

            entity.Property(e => e.ClaveAlumno)
                .HasComment("La asigna la universidad")
                .HasColumnName("clave_alumno");
            entity.Property(e => e.ApellidoMaterno)
                .HasMaxLength(50)
                .HasColumnName("apellido_materno");
            entity.Property(e => e.ApellidoPaterno)
                .HasMaxLength(50)
                .HasColumnName("apellido_paterno");
            entity.Property(e => e.CorreoInstitucional)
                .HasMaxLength(100)
                .HasColumnName("correo_institucional");
            entity.Property(e => e.CorreoPersonal)
                .HasMaxLength(100)
                .HasColumnName("correo_personal");
            entity.Property(e => e.FechaIngreso)
                .HasColumnType("date")
                .HasColumnName("fecha_ingreso");
            entity.Property(e => e.IdEstadoAcademico).HasColumnName("id_estado_academico");
            entity.Property(e => e.IdGeneracion).HasColumnName("id_generacion");
            entity.Property(e => e.IdModalidad)
                .HasComment("Nulo = Sin asignar")
                .HasColumnName("id_modalidad");
            entity.Property(e => e.IdPrograma).HasColumnName("id_programa");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
            entity.Property(e => e.PendienteConciliar)
                .HasComment("Alumno temporal: aún no está en el registro de la universidad")
                .HasColumnName("pendiente_conciliar");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdEstadoAcademicoNavigation).WithMany(p => p.Alumno)
                .HasForeignKey(d => d.IdEstadoAcademico)
                .HasConstraintName("fk_alumno_estado_academico");

            entity.HasOne(d => d.IdGeneracionNavigation).WithMany(p => p.Alumno)
                .HasForeignKey(d => d.IdGeneracion)
                .HasConstraintName("fk_alumno_generacion");

            entity.HasOne(d => d.IdModalidadNavigation).WithMany(p => p.Alumno)
                .HasForeignKey(d => d.IdModalidad)
                .HasConstraintName("fk_alumno_modalidad");

            entity.HasOne(d => d.IdProgramaNavigation).WithMany(p => p.Alumno)
                .HasForeignKey(d => d.IdPrograma)
                .HasConstraintName("fk_alumno_programa");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Alumno)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("fk_alumno_usuario");
        });

        modelBuilder.Entity<AlumnoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdAlumnoDetalle).HasName("PRIMARY");

            entity.ToTable("alumno_detalle");

            entity.HasIndex(e => e.IdPeriodo, "fk_detalle_periodo");

            entity.HasIndex(e => new { e.ClaveAlumno, e.IdPeriodo }, "uq_alumno_periodo").IsUnique();

            entity.Property(e => e.IdAlumnoDetalle).HasColumnName("id_alumno_detalle");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.FechaRegistro)
                .HasColumnType("date")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.IdPeriodo).HasColumnName("id_periodo");
            entity.Property(e => e.Semestre).HasColumnName("semestre");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.AlumnoDetalle)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_alumno");

            entity.HasOne(d => d.IdPeriodoNavigation).WithMany(p => p.AlumnoDetalle)
                .HasForeignKey(d => d.IdPeriodo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detalle_periodo");
        });

        modelBuilder.Entity<CatEstadoAcademico>(entity =>
        {
            entity.HasKey(e => e.IdEstadoAcademico).HasName("PRIMARY");

            entity.ToTable("CAT_estado_academico");

            entity.Property(e => e.IdEstadoAcademico).HasColumnName("id_estado_academico");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatEstadoEgresado>(entity =>
        {
            entity.HasKey(e => e.IdEstadoEgresado).HasName("PRIMARY");

            entity.ToTable("CAT_estado_egresado");

            entity.Property(e => e.IdEstadoEgresado).HasColumnName("id_estado_egresado");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatEstadoTesis>(entity =>
        {
            entity.HasKey(e => e.IdEstadoTesis).HasName("PRIMARY");

            entity.ToTable("CAT_estado_tesis");

            entity.Property(e => e.IdEstadoTesis).HasColumnName("id_estado_tesis");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatEstadoToefl>(entity =>
        {
            entity.HasKey(e => e.IdEstadoToefl).HasName("PRIMARY");

            entity.ToTable("CAT_estado_toefl");

            entity.Property(e => e.IdEstadoToefl).HasColumnName("id_estado_toefl");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatFormatoReporte>(entity =>
        {
            entity.HasKey(e => e.IdFormatoReporte).HasName("PRIMARY");

            entity.ToTable("CAT_formato_reporte");

            entity.Property(e => e.IdFormatoReporte).HasColumnName("id_formato_reporte");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatModalidadTitulacion>(entity =>
        {
            entity.HasKey(e => e.IdModalidad).HasName("PRIMARY");

            entity.ToTable("CAT_modalidad_titulacion");

            entity.Property(e => e.IdModalidad).HasColumnName("id_modalidad");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatProgramaAcademico>(entity =>
        {
            entity.HasKey(e => e.IdPrograma).HasName("PRIMARY");

            entity.ToTable("CAT_programa_academico");

            entity.Property(e => e.IdPrograma).HasColumnName("id_programa");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatRolComite>(entity =>
        {
            entity.HasKey(e => e.IdRolComite).HasName("PRIMARY");

            entity.ToTable("CAT_rol_comite");

            entity.Property(e => e.IdRolComite).HasColumnName("id_rol_comite");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatRolUsuario>(entity =>
        {
            entity.HasKey(e => e.IdRolUsuario).HasName("PRIMARY");

            entity.ToTable("CAT_rol_usuario");

            entity.Property(e => e.IdRolUsuario).HasColumnName("id_rol_usuario");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatSituacionLaboral>(entity =>
        {
            entity.HasKey(e => e.IdSituacionLaboral).HasName("PRIMARY");

            entity.ToTable("CAT_situacion_laboral");

            entity.Property(e => e.IdSituacionLaboral).HasColumnName("id_situacion_laboral");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatTipoDocumento>(entity =>
        {
            entity.HasKey(e => e.IdTipoDocumento).HasName("PRIMARY");

            entity.ToTable("CAT_tipo_documento");

            entity.Property(e => e.IdTipoDocumento).HasColumnName("id_tipo_documento");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CatTipoReporte>(entity =>
        {
            entity.HasKey(e => e.IdTipoReporte).HasName("PRIMARY");

            entity.ToTable("CAT_tipo_reporte");

            entity.Property(e => e.IdTipoReporte).HasColumnName("id_tipo_reporte");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Comite>(entity =>
        {
            entity.HasKey(e => e.IdComite).HasName("PRIMARY");

            entity.ToTable("comite");

            entity.HasIndex(e => e.Clave, "clave").IsUnique();

            entity.HasIndex(e => e.IdTesis, "fk_comite_tesis");

            entity.Property(e => e.IdComite).HasColumnName("id_comite");
            entity.Property(e => e.Clave)
                .HasMaxLength(50)
                .HasComment("CT-AAAA-NNNN, la genera el servidor")
                .HasColumnName("clave");
            entity.Property(e => e.IdTesis).HasColumnName("id_tesis");

            entity.HasOne(d => d.IdTesisNavigation).WithMany(p => p.Comite)
                .HasForeignKey(d => d.IdTesis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comite_tesis");
        });

        modelBuilder.Entity<ComiteProfesor>(entity =>
        {
            entity.HasKey(e => e.IdComiteProfesor).HasName("PRIMARY");

            entity.ToTable("comite_profesor");

            entity.HasIndex(e => e.IdComite, "fk_cp_comite");

            entity.HasIndex(e => e.Rpe, "fk_cp_profesor");

            entity.HasIndex(e => e.IdRolComite, "fk_cp_rol");

            entity.Property(e => e.IdComiteProfesor).HasColumnName("id_comite_profesor");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.FechaAsignacion)
                .HasColumnType("date")
                .HasColumnName("fecha_asignacion");
            entity.Property(e => e.FechaBaja)
                .HasColumnType("date")
                .HasColumnName("fecha_baja");
            entity.Property(e => e.IdComite).HasColumnName("id_comite");
            entity.Property(e => e.IdRolComite).HasColumnName("id_rol_comite");
            entity.Property(e => e.Rpe)
                .HasMaxLength(10)
                .HasColumnName("rpe");

            entity.HasOne(d => d.IdComiteNavigation).WithMany(p => p.ComiteProfesor)
                .HasForeignKey(d => d.IdComite)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cp_comite");

            entity.HasOne(d => d.IdRolComiteNavigation).WithMany(p => p.ComiteProfesor)
                .HasForeignKey(d => d.IdRolComite)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cp_rol");

            entity.HasOne(d => d.RpeNavigation).WithMany(p => p.ComiteProfesor)
                .HasForeignKey(d => d.Rpe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cp_profesor");
        });

        modelBuilder.Entity<DetalleEmpleo>(entity =>
        {
            entity.HasKey(e => e.IdDetalleEmpleo).HasName("PRIMARY");

            entity.ToTable("detalle_empleo");

            entity.HasIndex(e => e.IdEmpleo, "fk_detempleo_empleo");

            entity.Property(e => e.IdDetalleEmpleo).HasColumnName("id_detalle_empleo");
            entity.Property(e => e.Actividades)
                .HasColumnType("text")
                .HasColumnName("actividades");
            entity.Property(e => e.FechaFinal)
                .HasComment("Nula o futura = empleo vigente")
                .HasColumnType("date")
                .HasColumnName("fecha_final");
            entity.Property(e => e.FechaInicio)
                .HasColumnType("date")
                .HasColumnName("fecha_inicio");
            entity.Property(e => e.IdEmpleo).HasColumnName("id_empleo");
            entity.Property(e => e.Puesto)
                .HasMaxLength(100)
                .HasColumnName("puesto");
            entity.Property(e => e.RelacionPosgrado).HasColumnName("relacion_posgrado");
            entity.Property(e => e.Sueldo)
                .HasPrecision(10)
                .HasColumnName("sueldo");

            entity.HasOne(d => d.IdEmpleoNavigation).WithMany(p => p.DetalleEmpleo)
                .HasForeignKey(d => d.IdEmpleo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detempleo_empleo");
        });

        modelBuilder.Entity<Documento>(entity =>
        {
            entity.HasKey(e => e.IdDocumento).HasName("PRIMARY");

            entity.ToTable("documento");

            entity.HasIndex(e => e.CveActa, "fk_doc_acta");

            entity.HasIndex(e => e.ClaveAlumno, "fk_doc_alumno");

            entity.HasIndex(e => e.IdRevision, "fk_doc_revision");

            entity.HasIndex(e => e.IdTesis, "fk_doc_tesis");

            entity.HasIndex(e => e.IdTipoDocumento, "fk_doc_tipo");

            entity.HasIndex(e => e.IdUsuario, "fk_doc_usuario");

            entity.Property(e => e.IdDocumento).HasColumnName("id_documento");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.CveActa)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("cve_acta");
            entity.Property(e => e.FechaCarga)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_carga");
            entity.Property(e => e.IdRevision).HasColumnName("id_revision");
            entity.Property(e => e.IdTesis).HasColumnName("id_tesis");
            entity.Property(e => e.IdTipoDocumento).HasColumnName("id_tipo_documento");
            entity.Property(e => e.IdUsuario)
                .HasComment("Quién lo subió")
                .HasColumnName("id_usuario");
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(255)
                .HasColumnName("nombre_archivo");
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(255)
                .HasColumnName("ruta_archivo");
            entity.Property(e => e.Verificado)
                .HasComment("Constancia revisada por el coordinador")
                .HasColumnName("verificado");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.ClaveAlumno)
                .HasConstraintName("fk_doc_alumno");

            entity.HasOne(d => d.CveActaNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.CveActa)
                .HasConstraintName("fk_doc_acta");

            entity.HasOne(d => d.IdRevisionNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.IdRevision)
                .HasConstraintName("fk_doc_revision");

            entity.HasOne(d => d.IdTesisNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.IdTesis)
                .HasConstraintName("fk_doc_tesis");

            entity.HasOne(d => d.IdTipoDocumentoNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.IdTipoDocumento)
                .HasConstraintName("fk_doc_tipo");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Documento)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("fk_doc_usuario");
        });

        modelBuilder.Entity<Egresado>(entity =>
        {
            entity.HasKey(e => e.ClaveAlumno).HasName("PRIMARY");

            entity.ToTable("egresado");

            entity.HasIndex(e => e.IdEstadoEgresado, "fk_egresado_estado");

            entity.HasIndex(e => e.IdentificadorEgresado, "identificador_egresado").IsUnique();

            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .HasComment("Correo personal = su usuario")
                .HasColumnName("correo");
            entity.Property(e => e.FechaEgreso)
                .HasColumnType("date")
                .HasColumnName("fecha_egreso");
            entity.Property(e => e.IdEstadoEgresado).HasColumnName("id_estado_egresado");
            entity.Property(e => e.IdentificadorEgresado)
                .HasMaxLength(50)
                .HasComment("Generación + iniciales + consecutivo, p. ej. 2022RSC01")
                .HasColumnName("identificador_egresado");
            entity.Property(e => e.OtrosContactos)
                .HasMaxLength(255)
                .HasColumnName("otros_contactos");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
            entity.Property(e => e.UltimaActualizacion)
                .HasColumnType("date")
                .HasColumnName("ultima_actualizacion");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithOne(p => p.Egresado)
                .HasForeignKey<Egresado>(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_egresado_alumno");

            entity.HasOne(d => d.IdEstadoEgresadoNavigation).WithMany(p => p.Egresado)
                .HasForeignKey(d => d.IdEstadoEgresado)
                .HasConstraintName("fk_egresado_estado");
        });

        modelBuilder.Entity<Empleo>(entity =>
        {
            entity.HasKey(e => e.IdEmpleo).HasName("PRIMARY");

            entity.ToTable("empleo");

            entity.HasIndex(e => e.ClaveAlumno, "fk_empleo_egresado");

            entity.HasIndex(e => e.IdEmpresa, "fk_empleo_empresa");

            entity.Property(e => e.IdEmpleo).HasColumnName("id_empleo");
            entity.Property(e => e.AreaProfesional)
                .HasMaxLength(100)
                .HasColumnName("area_profesional");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .HasColumnName("tipo");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.Empleo)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_empleo_egresado");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.Empleo)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("fk_empleo_empresa");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.IdEmpresa).HasName("PRIMARY");

            entity.ToTable("empresa");

            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.Estado)
                .HasMaxLength(100)
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.Pais)
                .HasMaxLength(100)
                .HasColumnName("pais");
            entity.Property(e => e.Sector)
                .HasMaxLength(100)
                .HasColumnName("sector");
        });

        modelBuilder.Entity<Generacion>(entity =>
        {
            entity.HasKey(e => e.IdGeneracion).HasName("PRIMARY");

            entity.ToTable("generacion");

            entity.HasIndex(e => e.IdPrograma, "fk_generacion_programa");

            entity.Property(e => e.IdGeneracion).HasColumnName("id_generacion");
            entity.Property(e => e.AnioEgresoEstimado).HasColumnName("anio_egreso_estimado");
            entity.Property(e => e.AnioIngreso).HasColumnName("anio_ingreso");
            entity.Property(e => e.IdPrograma).HasColumnName("id_programa");
            entity.Property(e => e.NombreGeneracion)
                .HasMaxLength(100)
                .HasColumnName("nombre_generacion");

            entity.HasOne(d => d.IdProgramaNavigation).WithMany(p => p.Generacion)
                .HasForeignKey(d => d.IdPrograma)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_generacion_programa");
        });

        modelBuilder.Entity<IntentoContacto>(entity =>
        {
            entity.HasKey(e => e.IdIntento).HasName("PRIMARY");

            entity.ToTable("intento_contacto");

            entity.HasIndex(e => e.ClaveAlumno, "fk_intento_egresado");

            entity.Property(e => e.IdIntento).HasColumnName("id_intento");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.FechaIntento)
                .HasColumnType("date")
                .HasColumnName("fecha_intento");
            entity.Property(e => e.Observaciones)
                .HasColumnType("text")
                .HasColumnName("observaciones");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.IntentoContacto)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_intento_egresado");
        });

        modelBuilder.Entity<LogAcceso>(entity =>
        {
            entity.HasKey(e => e.IdLog).HasName("PRIMARY");

            entity.ToTable("log_acceso");

            entity.HasIndex(e => e.IdUsuario, "fk_log_usuario");

            entity.Property(e => e.IdLog).HasColumnName("id_log");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Ip)
                .HasMaxLength(45)
                .HasColumnName("ip");
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .HasComment("Correcto | Contraseña incorrecta | Bloqueado | Primer acceso")
                .HasColumnName("resultado");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.LogAcceso)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("fk_log_usuario");
        });

        modelBuilder.Entity<PeriodoAcademico>(entity =>
        {
            entity.HasKey(e => e.IdPeriodo).HasName("PRIMARY");

            entity.ToTable("periodo_academico");

            entity.Property(e => e.IdPeriodo).HasColumnName("id_periodo");
            entity.Property(e => e.FechaFin)
                .HasColumnType("date")
                .HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio)
                .HasColumnType("date")
                .HasColumnName("fecha_inicio");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.TipoPeriodo)
                .HasMaxLength(50)
                .HasColumnName("tipo_periodo");
        });

        modelBuilder.Entity<Profesor>(entity =>
        {
            entity.HasKey(e => e.Rpe).HasName("PRIMARY");

            entity.ToTable("profesor");

            entity.Property(e => e.Rpe)
                .HasMaxLength(10)
                .HasComment("RPE de la universidad o EXT-NNN para externos")
                .HasColumnName("rpe");
            entity.Property(e => e.ApellidoMaterno)
                .HasMaxLength(50)
                .HasColumnName("apellido_materno");
            entity.Property(e => e.ApellidoPaterno)
                .HasMaxLength(50)
                .HasColumnName("apellido_paterno");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<ProfesorDetalle>(entity =>
        {
            entity.HasKey(e => e.IdProfesorDetalle).HasName("PRIMARY");

            entity.ToTable("profesor_detalle");

            entity.HasIndex(e => e.Rpe, "fk_profdet_profesor");

            entity.HasIndex(e => e.IdUsuario, "fk_profdet_usuario");

            entity.Property(e => e.IdProfesorDetalle).HasColumnName("id_profesor_detalle");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.CorreoInstitucional)
                .HasMaxLength(100)
                .HasColumnName("correo_institucional");
            entity.Property(e => e.Externo).HasColumnName("externo");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Institucion)
                .HasMaxLength(150)
                .HasComment("Solo externos")
                .HasColumnName("institucion");
            entity.Property(e => e.Rpe)
                .HasMaxLength(10)
                .HasColumnName("rpe");
            entity.Property(e => e.VigenteHasta)
                .HasComment("Solo externos: al vencer se desactiva")
                .HasColumnType("date")
                .HasColumnName("vigente_hasta");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ProfesorDetalle)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("fk_profdet_usuario");

            entity.HasOne(d => d.RpeNavigation).WithMany(p => p.ProfesorDetalle)
                .HasForeignKey(d => d.Rpe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_profdet_profesor");
        });

        modelBuilder.Entity<Reporte>(entity =>
        {
            entity.HasKey(e => e.IdReporte).HasName("PRIMARY");

            entity.ToTable("reporte");

            entity.HasIndex(e => e.IdFormatoReporte, "fk_reporte_formato");

            entity.HasIndex(e => e.IdTipoReporte, "fk_reporte_tipo");

            entity.HasIndex(e => e.IdUsuarioGenero, "fk_reporte_usuario");

            entity.Property(e => e.IdReporte).HasColumnName("id_reporte");
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_generacion");
            entity.Property(e => e.FiltrosUsados)
                .HasColumnType("text")
                .HasColumnName("filtros_usados");
            entity.Property(e => e.IdFormatoReporte).HasColumnName("id_formato_reporte");
            entity.Property(e => e.IdTipoReporte).HasColumnName("id_tipo_reporte");
            entity.Property(e => e.IdUsuarioGenero).HasColumnName("id_usuario_genero");
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(255)
                .HasColumnName("ruta_archivo");

            entity.HasOne(d => d.IdFormatoReporteNavigation).WithMany(p => p.Reporte)
                .HasForeignKey(d => d.IdFormatoReporte)
                .HasConstraintName("fk_reporte_formato");

            entity.HasOne(d => d.IdTipoReporteNavigation).WithMany(p => p.Reporte)
                .HasForeignKey(d => d.IdTipoReporte)
                .HasConstraintName("fk_reporte_tipo");

            entity.HasOne(d => d.IdUsuarioGeneroNavigation).WithMany(p => p.Reporte)
                .HasForeignKey(d => d.IdUsuarioGenero)
                .HasConstraintName("fk_reporte_usuario");
        });

        modelBuilder.Entity<RequisitoToefl>(entity =>
        {
            entity.HasKey(e => e.IdToefl).HasName("PRIMARY");

            entity.ToTable("requisito_toefl");

            entity.HasIndex(e => e.ClaveAlumno, "fk_toefl_alumno");

            entity.HasIndex(e => e.IdEstadoToefl, "fk_toefl_estado");

            entity.Property(e => e.IdToefl).HasColumnName("id_toefl");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.FechaPresentacion)
                .HasComment("La registra el alumno")
                .HasColumnType("date")
                .HasColumnName("fecha_presentacion");
            entity.Property(e => e.FechaProxima)
                .HasComment("Próxima fecha para presentar; la registra el alumno")
                .HasColumnType("date")
                .HasColumnName("fecha_proxima");
            entity.Property(e => e.IdEstadoToefl).HasColumnName("id_estado_toefl");
            entity.Property(e => e.Observaciones)
                .HasColumnType("text")
                .HasColumnName("observaciones");
            entity.Property(e => e.Puntaje)
                .HasPrecision(5)
                .HasComment("Lo captura el coordinador al aprobar")
                .HasColumnName("puntaje");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.RequisitoToefl)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_toefl_alumno");

            entity.HasOne(d => d.IdEstadoToeflNavigation).WithMany(p => p.RequisitoToefl)
                .HasForeignKey(d => d.IdEstadoToefl)
                .HasConstraintName("fk_toefl_estado");
        });

        modelBuilder.Entity<RetroalimentacionEgresado>(entity =>
        {
            entity.HasKey(e => e.IdRetroalimentacion).HasName("PRIMARY");

            entity.ToTable("retroalimentacion_egresado");

            entity.HasIndex(e => e.ClaveAlumno, "fk_retro_egresado");

            entity.Property(e => e.IdRetroalimentacion).HasColumnName("id_retroalimentacion");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.Comentario)
                .HasColumnType("text")
                .HasColumnName("comentario");
            entity.Property(e => e.Fecha)
                .HasColumnType("date")
                .HasColumnName("fecha");
            entity.Property(e => e.Sugerencia)
                .HasColumnType("text")
                .HasColumnName("sugerencia");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.RetroalimentacionEgresado)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_retro_egresado");
        });

        modelBuilder.Entity<Revision>(entity =>
        {
            entity.HasKey(e => e.IdRevision).HasName("PRIMARY");

            entity.ToTable("revision");

            entity.HasIndex(e => e.IdPeriodo, "fk_revision_periodo");

            entity.HasIndex(e => e.IdTesis, "fk_revision_tesis");

            entity.Property(e => e.IdRevision).HasColumnName("id_revision");
            entity.Property(e => e.CalifDocumento).HasColumnName("calif_documento");
            entity.Property(e => e.CalifDominio).HasColumnName("calif_dominio");
            entity.Property(e => e.CalifExposicion).HasColumnName("calif_exposicion");
            entity.Property(e => e.CalifGradoAvance).HasColumnName("calif_grado_avance");
            entity.Property(e => e.Comentarios)
                .HasColumnType("text")
                .HasColumnName("comentarios");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Abierta'")
                .HasColumnName("estado");
            entity.Property(e => e.Fecha)
                .HasColumnType("date")
                .HasColumnName("fecha");
            entity.Property(e => e.IdPeriodo).HasColumnName("id_periodo");
            entity.Property(e => e.IdTesis).HasColumnName("id_tesis");
            entity.Property(e => e.PorcentajeAvance)
                .HasPrecision(5)
                .HasColumnName("porcentaje_avance");

            entity.HasOne(d => d.IdPeriodoNavigation).WithMany(p => p.Revision)
                .HasForeignKey(d => d.IdPeriodo)
                .HasConstraintName("fk_revision_periodo");

            entity.HasOne(d => d.IdTesisNavigation).WithMany(p => p.Revision)
                .HasForeignKey(d => d.IdTesis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_revision_tesis");
        });

        modelBuilder.Entity<RevisionProfesor>(entity =>
        {
            entity.HasKey(e => e.IdRevisionProfesor).HasName("PRIMARY");

            entity.ToTable("revision_profesor");

            entity.HasIndex(e => e.Rpe, "fk_rp_profesor");

            entity.HasIndex(e => new { e.IdRevision, e.Rpe }, "uq_revision_profesor").IsUnique();

            entity.Property(e => e.IdRevisionProfesor).HasColumnName("id_revision_profesor");
            entity.Property(e => e.CalifDocumento).HasColumnName("calif_documento");
            entity.Property(e => e.CalifDominio).HasColumnName("calif_dominio");
            entity.Property(e => e.CalifExposicion).HasColumnName("calif_exposicion");
            entity.Property(e => e.CalifGradoAvance).HasColumnName("calif_grado_avance");
            entity.Property(e => e.Comentarios)
                .HasColumnType("text")
                .HasColumnName("comentarios");
            entity.Property(e => e.FechaConformidad)
                .HasComment("Su firma del acta")
                .HasColumnType("datetime")
                .HasColumnName("fecha_conformidad");
            entity.Property(e => e.FechaEnvio)
                .HasComment("Nulo mientras no envía su revisión")
                .HasColumnType("datetime")
                .HasColumnName("fecha_envio");
            entity.Property(e => e.IdRevision).HasColumnName("id_revision");
            entity.Property(e => e.PorcentajeAvance)
                .HasPrecision(5)
                .HasColumnName("porcentaje_avance");
            entity.Property(e => e.Rpe)
                .HasMaxLength(10)
                .HasColumnName("rpe");

            entity.HasOne(d => d.IdRevisionNavigation).WithMany(p => p.RevisionProfesor)
                .HasForeignKey(d => d.IdRevision)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rp_revision");

            entity.HasOne(d => d.RpeNavigation).WithMany(p => p.RevisionProfesor)
                .HasForeignKey(d => d.Rpe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rp_profesor");
        });

        modelBuilder.Entity<SesionUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("sesion_usuario");

            entity.HasIndex(e => e.UsuarioId, "ix_sesion_usuario");

            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Creada).HasMaxLength(6);
            entity.Property(e => e.UltimaActividad).HasMaxLength(6);

            entity.HasOne(d => d.Usuario).WithMany(p => p.SesionUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sesion_usuario");
        });

        modelBuilder.Entity<Tesis>(entity =>
        {
            entity.HasKey(e => e.IdTesis).HasName("PRIMARY");

            entity.ToTable("tesis");

            entity.HasIndex(e => e.CveActaResolucion, "fk_tesis_acta_resolucion");

            entity.HasIndex(e => e.ClaveAlumno, "fk_tesis_alumno");

            entity.HasIndex(e => e.RpeDirector, "fk_tesis_director");

            entity.HasIndex(e => e.IdEstadoTesis, "fk_tesis_estado");

            entity.HasIndex(e => e.IdModalidad, "fk_tesis_modalidad");

            entity.HasIndex(e => e.IdPrograma, "fk_tesis_programa");

            entity.HasIndex(e => new { e.EstadoSolicitud, e.RpeDirector }, "ix_tesis_solicitud");

            entity.Property(e => e.IdTesis).HasColumnName("id_tesis");
            entity.Property(e => e.ClaveAlumno).HasColumnName("clave_alumno");
            entity.Property(e => e.CveActaResolucion)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("cve_acta_resolucion");
            entity.Property(e => e.EstadoSolicitud)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pendiente'")
                .HasColumnName("estado_solicitud");
            entity.Property(e => e.FechaCierre)
                .HasComment("Resolución final, la registra el director")
                .HasColumnType("date")
                .HasColumnName("fecha_cierre");
            entity.Property(e => e.FechaRegistro)
                .HasColumnType("date")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.FechaResolucion)
                .HasComment("Cuándo aceptó o rechazó el director")
                .HasColumnType("date")
                .HasColumnName("fecha_resolucion");
            entity.Property(e => e.FechaSolicitud)
                .HasColumnType("date")
                .HasColumnName("fecha_solicitud");
            entity.Property(e => e.IdEstadoTesis).HasColumnName("id_estado_tesis");
            entity.Property(e => e.IdModalidad)
                .HasComment("Queda fijada al aceptarse la solicitud")
                .HasColumnName("id_modalidad");
            entity.Property(e => e.IdPrograma).HasColumnName("id_programa");
            entity.Property(e => e.MotivoRechazo)
                .HasMaxLength(255)
                .HasColumnName("motivo_rechazo");
            entity.Property(e => e.PorcentajeActual)
                .HasPrecision(5)
                .HasColumnName("porcentaje_actual");
            entity.Property(e => e.RpeDirector)
                .HasMaxLength(10)
                .HasColumnName("rpe_director");
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .HasColumnName("titulo");
            entity.Property(e => e.UltimaActualizacion)
                .HasColumnType("datetime")
                .HasColumnName("ultima_actualizacion");

            entity.HasOne(d => d.ClaveAlumnoNavigation).WithMany(p => p.Tesis)
                .HasForeignKey(d => d.ClaveAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tesis_alumno");

            entity.HasOne(d => d.CveActaResolucionNavigation).WithMany(p => p.Tesis)
                .HasForeignKey(d => d.CveActaResolucion)
                .HasConstraintName("fk_tesis_acta_resolucion");

            entity.HasOne(d => d.IdEstadoTesisNavigation).WithMany(p => p.Tesis)
                .HasForeignKey(d => d.IdEstadoTesis)
                .HasConstraintName("fk_tesis_estado");

            entity.HasOne(d => d.IdModalidadNavigation).WithMany(p => p.Tesis)
                .HasForeignKey(d => d.IdModalidad)
                .HasConstraintName("fk_tesis_modalidad");

            entity.HasOne(d => d.IdProgramaNavigation).WithMany(p => p.Tesis)
                .HasForeignKey(d => d.IdPrograma)
                .HasConstraintName("fk_tesis_programa");

            entity.HasOne(d => d.RpeDirectorNavigation).WithMany(p => p.Tesis)
                .HasForeignKey(d => d.RpeDirector)
                .HasConstraintName("fk_tesis_director");
        });

        modelBuilder.Entity<TokenRecuperacion>(entity =>
        {
            entity.HasKey(e => e.IdToken).HasName("PRIMARY");

            entity.ToTable("token_recuperacion");

            entity.HasIndex(e => e.IdUsuario, "fk_token_usuario");

            entity.HasIndex(e => e.Token, "token").IsUnique();

            entity.Property(e => e.IdToken).HasColumnName("id_token");
            entity.Property(e => e.FechaExpiracion)
                .HasColumnType("datetime")
                .HasColumnName("fecha_expiracion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Token)
                .HasComment("Hash del token, nunca el token")
                .HasColumnName("token");
            entity.Property(e => e.Usado)
                .HasDefaultValueSql("'0'")
                .HasColumnName("usado");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TokenRecuperacion)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_token_usuario");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.ClaveInstitucional, "clave_institucional").IsUnique();

            entity.HasIndex(e => e.Correo, "correo").IsUnique();

            entity.HasIndex(e => e.IdRolUsuario, "fk_usuario_rol");

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Activo)
                .HasDefaultValueSql("'1'")
                .HasColumnName("activo");
            entity.Property(e => e.BloqueadoHasta)
                .HasColumnType("datetime")
                .HasColumnName("bloqueado_hasta");
            entity.Property(e => e.ClaveInstitucional)
                .HasMaxLength(30)
                .HasComment("RPE, clave de alumno, EXT-… o identificador de egresado")
                .HasColumnName("clave_institucional");
            entity.Property(e => e.ContrasenaHash)
                .HasMaxLength(255)
                .HasColumnName("contrasena_hash");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .HasColumnName("correo");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.IdRolUsuario).HasColumnName("id_rol_usuario");
            entity.Property(e => e.IntentosFallidos)
                .HasDefaultValueSql("'0'")
                .HasColumnName("intentos_fallidos");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.RequiereCambio)
                .HasDefaultValueSql("'0'")
                .HasColumnName("requiere_cambio");
            entity.Property(e => e.UltimoAcceso)
                .HasColumnType("datetime")
                .HasColumnName("ultimo_acceso");

            entity.HasOne(d => d.IdRolUsuarioNavigation).WithMany(p => p.Usuario)
                .HasForeignKey(d => d.IdRolUsuario)
                .HasConstraintName("fk_usuario_rol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
