using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_estado_academico",
                columns: table => new
                {
                    id_estado_academico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_estado_academico);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_estado_egresado",
                columns: table => new
                {
                    id_estado_egresado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_estado_egresado);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_estado_tesis",
                columns: table => new
                {
                    id_estado_tesis = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_estado_tesis);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_estado_toefl",
                columns: table => new
                {
                    id_estado_toefl = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_estado_toefl);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_formato_reporte",
                columns: table => new
                {
                    id_formato_reporte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_formato_reporte);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_modalidad_titulacion",
                columns: table => new
                {
                    id_modalidad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_modalidad);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_programa_academico",
                columns: table => new
                {
                    id_programa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_programa);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_rol_comite",
                columns: table => new
                {
                    id_rol_comite = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_rol_comite);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_rol_usuario",
                columns: table => new
                {
                    id_rol_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_rol_usuario);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_situacion_laboral",
                columns: table => new
                {
                    id_situacion_laboral = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_situacion_laboral);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_tipo_documento",
                columns: table => new
                {
                    id_tipo_documento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_tipo_documento);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CAT_tipo_reporte",
                columns: table => new
                {
                    id_tipo_reporte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_tipo_reporte);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "empresa",
                columns: table => new
                {
                    id_empresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    sector = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    pais = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    estado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_empresa);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "periodo_academico",
                columns: table => new
                {
                    id_periodo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: true),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: true),
                    tipo_periodo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_periodo);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "profesor",
                columns: table => new
                {
                    rpe = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, comment: "RPE de la universidad o EXT-NNN para externos"),
                    nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    apellido_paterno = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    apellido_materno = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.rpe);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "generacion",
                columns: table => new
                {
                    id_generacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_programa = table.Column<int>(type: "int", nullable: false),
                    anio_ingreso = table.Column<int>(type: "int", nullable: true),
                    anio_egreso_estimado = table.Column<int>(type: "int", nullable: true),
                    nombre_generacion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_generacion);
                    table.ForeignKey(
                        name: "fk_generacion_programa",
                        column: x => x.id_programa,
                        principalTable: "CAT_programa_academico",
                        principalColumn: "id_programa");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    correo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    contrasena_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    id_rol_usuario = table.Column<int>(type: "int", nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    intentos_fallidos = table.Column<int>(type: "int", nullable: true, defaultValueSql: "'0'"),
                    bloqueado_hasta = table.Column<DateTime>(type: "datetime", nullable: true),
                    requiere_cambio = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ultimo_acceso = table.Column<DateTime>(type: "datetime", nullable: true),
                    clave_institucional = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true, comment: "RPE, clave de alumno, EXT-… o identificador de egresado"),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_usuario);
                    table.ForeignKey(
                        name: "fk_usuario_rol",
                        column: x => x.id_rol_usuario,
                        principalTable: "CAT_rol_usuario",
                        principalColumn: "id_rol_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "alumno",
                columns: table => new
                {
                    clave_alumno = table.Column<int>(type: "int", nullable: false, comment: "La asigna la universidad"),
                    nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    apellido_paterno = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    apellido_materno = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    fecha_ingreso = table.Column<DateTime>(type: "date", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    id_programa = table.Column<int>(type: "int", nullable: true),
                    id_generacion = table.Column<int>(type: "int", nullable: true),
                    correo_institucional = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    correo_personal = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    telefono = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    id_modalidad = table.Column<int>(type: "int", nullable: true, comment: "Nulo = Sin asignar"),
                    id_estado_academico = table.Column<int>(type: "int", nullable: true),
                    pendiente_conciliar = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "Alumno temporal: aún no está en el registro de la universidad")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.clave_alumno);
                    table.ForeignKey(
                        name: "fk_alumno_estado_academico",
                        column: x => x.id_estado_academico,
                        principalTable: "CAT_estado_academico",
                        principalColumn: "id_estado_academico");
                    table.ForeignKey(
                        name: "fk_alumno_generacion",
                        column: x => x.id_generacion,
                        principalTable: "generacion",
                        principalColumn: "id_generacion");
                    table.ForeignKey(
                        name: "fk_alumno_modalidad",
                        column: x => x.id_modalidad,
                        principalTable: "CAT_modalidad_titulacion",
                        principalColumn: "id_modalidad");
                    table.ForeignKey(
                        name: "fk_alumno_programa",
                        column: x => x.id_programa,
                        principalTable: "CAT_programa_academico",
                        principalColumn: "id_programa");
                    table.ForeignKey(
                        name: "fk_alumno_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "log_acceso",
                columns: table => new
                {
                    id_log = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    resultado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true, comment: "Correcto | Contraseña incorrecta | Bloqueado | Primer acceso"),
                    ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_log);
                    table.ForeignKey(
                        name: "fk_log_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "profesor_detalle",
                columns: table => new
                {
                    id_profesor_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    rpe = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    correo_institucional = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    externo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    institucion = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, comment: "Solo externos"),
                    vigente_hasta = table.Column<DateTime>(type: "date", nullable: true, comment: "Solo externos: al vencer se desactiva")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_profesor_detalle);
                    table.ForeignKey(
                        name: "fk_profdet_profesor",
                        column: x => x.rpe,
                        principalTable: "profesor",
                        principalColumn: "rpe");
                    table.ForeignKey(
                        name: "fk_profdet_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reporte",
                columns: table => new
                {
                    id_reporte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_tipo_reporte = table.Column<int>(type: "int", nullable: true),
                    id_formato_reporte = table.Column<int>(type: "int", nullable: true),
                    id_usuario_genero = table.Column<int>(type: "int", nullable: true),
                    fecha_generacion = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    filtros_usados = table.Column<string>(type: "text", nullable: true),
                    ruta_archivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_reporte);
                    table.ForeignKey(
                        name: "fk_reporte_formato",
                        column: x => x.id_formato_reporte,
                        principalTable: "CAT_formato_reporte",
                        principalColumn: "id_formato_reporte");
                    table.ForeignKey(
                        name: "fk_reporte_tipo",
                        column: x => x.id_tipo_reporte,
                        principalTable: "CAT_tipo_reporte",
                        principalColumn: "id_tipo_reporte");
                    table.ForeignKey(
                        name: "fk_reporte_usuario",
                        column: x => x.id_usuario_genero,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sesion_usuario",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    UltimaActividad = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false),
                    Creada = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                    table.ForeignKey(
                        name: "fk_sesion_usuario",
                        column: x => x.UsuarioId,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "token_recuperacion",
                columns: table => new
                {
                    id_token = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    token = table.Column<string>(type: "varchar(255)", nullable: false, comment: "Hash del token, nunca el token"),
                    fecha_expiracion = table.Column<DateTime>(type: "datetime", nullable: false),
                    usado = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_token);
                    table.ForeignKey(
                        name: "fk_token_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "alumno_detalle",
                columns: table => new
                {
                    id_alumno_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    id_periodo = table.Column<int>(type: "int", nullable: false),
                    semestre = table.Column<int>(type: "int", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_alumno_detalle);
                    table.ForeignKey(
                        name: "fk_detalle_alumno",
                        column: x => x.clave_alumno,
                        principalTable: "alumno",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_detalle_periodo",
                        column: x => x.id_periodo,
                        principalTable: "periodo_academico",
                        principalColumn: "id_periodo");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "egresado",
                columns: table => new
                {
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    identificador_egresado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, comment: "Generación + iniciales + consecutivo, p. ej. 2022RSC01"),
                    fecha_egreso = table.Column<DateTime>(type: "date", nullable: true),
                    id_estado_egresado = table.Column<int>(type: "int", nullable: true),
                    correo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, comment: "Correo personal = su usuario"),
                    telefono = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    otros_contactos = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ultima_actualizacion = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.clave_alumno);
                    table.ForeignKey(
                        name: "fk_egresado_alumno",
                        column: x => x.clave_alumno,
                        principalTable: "alumno",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_egresado_estado",
                        column: x => x.id_estado_egresado,
                        principalTable: "CAT_estado_egresado",
                        principalColumn: "id_estado_egresado");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "requisito_toefl",
                columns: table => new
                {
                    id_toefl = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    id_estado_toefl = table.Column<int>(type: "int", nullable: true),
                    fecha_presentacion = table.Column<DateTime>(type: "date", nullable: true, comment: "La registra el alumno"),
                    puntaje = table.Column<decimal>(type: "decimal(5,2)", precision: 5, nullable: true, comment: "Lo captura el coordinador al aprobar"),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    fecha_proxima = table.Column<DateTime>(type: "date", nullable: true, comment: "Próxima fecha para presentar; la registra el alumno")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_toefl);
                    table.ForeignKey(
                        name: "fk_toefl_alumno",
                        column: x => x.clave_alumno,
                        principalTable: "alumno",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_toefl_estado",
                        column: x => x.id_estado_toefl,
                        principalTable: "CAT_estado_toefl",
                        principalColumn: "id_estado_toefl");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "empleo",
                columns: table => new
                {
                    id_empleo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    id_empresa = table.Column<int>(type: "int", nullable: true),
                    tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    area_profesional = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_empleo);
                    table.CheckConstraint("ck_empleo_tipo", "tipo IN ('Empleo', 'Beca', 'Otro')");
                    table.ForeignKey(
                        name: "fk_empleo_egresado",
                        column: x => x.clave_alumno,
                        principalTable: "egresado",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_empleo_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresa",
                        principalColumn: "id_empresa");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "intento_contacto",
                columns: table => new
                {
                    id_intento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    fecha_intento = table.Column<DateTime>(type: "date", nullable: true),
                    observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_intento);
                    table.ForeignKey(
                        name: "fk_intento_egresado",
                        column: x => x.clave_alumno,
                        principalTable: "egresado",
                        principalColumn: "clave_alumno");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "retroalimentacion_egresado",
                columns: table => new
                {
                    id_retroalimentacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: true),
                    comentario = table.Column<string>(type: "text", nullable: true),
                    sugerencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_retroalimentacion);
                    table.ForeignKey(
                        name: "fk_retro_egresado",
                        column: x => x.clave_alumno,
                        principalTable: "egresado",
                        principalColumn: "clave_alumno");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "detalle_empleo",
                columns: table => new
                {
                    id_detalle_empleo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_empleo = table.Column<int>(type: "int", nullable: false),
                    puesto = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: true),
                    fecha_final = table.Column<DateTime>(type: "date", nullable: true, comment: "Nula o futura = empleo vigente"),
                    sueldo = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    relacion_posgrado = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    actividades = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_detalle_empleo);
                    table.ForeignKey(
                        name: "fk_detempleo_empleo",
                        column: x => x.id_empleo,
                        principalTable: "empleo",
                        principalColumn: "id_empleo");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acta_comite",
                columns: table => new
                {
                    cve_acta = table.Column<string>(type: "char(6)", fixedLength: true, maxLength: 6, nullable: false),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    id_comite = table.Column<int>(type: "int", nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: true),
                    id_toefl = table.Column<int>(type: "int", nullable: true),
                    num_evaluacion = table.Column<int>(type: "int", nullable: true),
                    fecha_reunion = table.Column<DateTime>(type: "date", nullable: true),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    resultado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    archivo_acta = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    estado = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.cve_acta);
                    table.ForeignKey(
                        name: "fk_acta_alumno",
                        column: x => x.clave_alumno,
                        principalTable: "alumno",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_acta_toefl",
                        column: x => x.id_toefl,
                        principalTable: "requisito_toefl",
                        principalColumn: "id_toefl");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tesis",
                columns: table => new
                {
                    id_tesis = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    titulo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    clave_alumno = table.Column<int>(type: "int", nullable: false),
                    id_programa = table.Column<int>(type: "int", nullable: true),
                    porcentaje_actual = table.Column<decimal>(type: "decimal(5,2)", precision: 5, nullable: true),
                    id_estado_tesis = table.Column<int>(type: "int", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "date", nullable: true),
                    linea_investigacion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    id_modalidad = table.Column<int>(type: "int", nullable: true, comment: "Queda fijada al aceptarse la solicitud"),
                    estado_solicitud = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "'Pendiente'"),
                    rpe_director = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    fecha_solicitud = table.Column<DateTime>(type: "date", nullable: true),
                    fecha_resolucion = table.Column<DateTime>(type: "date", nullable: true, comment: "Cuándo aceptó o rechazó el director"),
                    motivo_rechazo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ultima_actualizacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    fecha_cierre = table.Column<DateTime>(type: "date", nullable: true, comment: "Resolución final, la registra el director"),
                    cve_acta_resolucion = table.Column<string>(type: "char(6)", fixedLength: true, maxLength: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_tesis);
                    table.CheckConstraint("ck_tesis_solicitud", "estado_solicitud IN ('Pendiente', 'Aceptada', 'Rechazada')");
                    table.ForeignKey(
                        name: "fk_tesis_acta_resolucion",
                        column: x => x.cve_acta_resolucion,
                        principalTable: "acta_comite",
                        principalColumn: "cve_acta");
                    table.ForeignKey(
                        name: "fk_tesis_alumno",
                        column: x => x.clave_alumno,
                        principalTable: "alumno",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_tesis_director",
                        column: x => x.rpe_director,
                        principalTable: "profesor",
                        principalColumn: "rpe");
                    table.ForeignKey(
                        name: "fk_tesis_estado",
                        column: x => x.id_estado_tesis,
                        principalTable: "CAT_estado_tesis",
                        principalColumn: "id_estado_tesis");
                    table.ForeignKey(
                        name: "fk_tesis_modalidad",
                        column: x => x.id_modalidad,
                        principalTable: "CAT_modalidad_titulacion",
                        principalColumn: "id_modalidad");
                    table.ForeignKey(
                        name: "fk_tesis_programa",
                        column: x => x.id_programa,
                        principalTable: "CAT_programa_academico",
                        principalColumn: "id_programa");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "comite",
                columns: table => new
                {
                    id_comite = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_tesis = table.Column<int>(type: "int", nullable: false),
                    clave = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, comment: "CT-AAAA-NNNN, la genera el servidor")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_comite);
                    table.ForeignKey(
                        name: "fk_comite_tesis",
                        column: x => x.id_tesis,
                        principalTable: "tesis",
                        principalColumn: "id_tesis");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "revision",
                columns: table => new
                {
                    id_revision = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    fecha = table.Column<DateTime>(type: "date", nullable: true),
                    id_tesis = table.Column<int>(type: "int", nullable: false),
                    id_periodo = table.Column<int>(type: "int", nullable: true),
                    comentarios = table.Column<string>(type: "text", nullable: true),
                    porcentaje_avance = table.Column<decimal>(type: "decimal(5,2)", precision: 5, nullable: true),
                    estado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValueSql: "'Abierta'"),
                    calif_documento = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    calif_exposicion = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    calif_dominio = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    calif_grado_avance = table.Column<byte>(type: "tinyint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_revision);
                    table.CheckConstraint("ck_revision_calif", "calif_documento <= 100 AND calif_exposicion <= 100 AND calif_dominio <= 100 AND calif_grado_avance <= 100");
                    table.CheckConstraint("ck_revision_estado", "estado IN ('Abierta', 'En consolidación', 'En firma', 'Cerrada')");
                    table.ForeignKey(
                        name: "fk_revision_periodo",
                        column: x => x.id_periodo,
                        principalTable: "periodo_academico",
                        principalColumn: "id_periodo");
                    table.ForeignKey(
                        name: "fk_revision_tesis",
                        column: x => x.id_tesis,
                        principalTable: "tesis",
                        principalColumn: "id_tesis");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "comite_profesor",
                columns: table => new
                {
                    id_comite_profesor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_comite = table.Column<int>(type: "int", nullable: false),
                    rpe = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    id_rol_comite = table.Column<int>(type: "int", nullable: false),
                    fecha_asignacion = table.Column<DateTime>(type: "date", nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    fecha_baja = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_comite_profesor);
                    table.ForeignKey(
                        name: "fk_cp_comite",
                        column: x => x.id_comite,
                        principalTable: "comite",
                        principalColumn: "id_comite");
                    table.ForeignKey(
                        name: "fk_cp_profesor",
                        column: x => x.rpe,
                        principalTable: "profesor",
                        principalColumn: "rpe");
                    table.ForeignKey(
                        name: "fk_cp_rol",
                        column: x => x.id_rol_comite,
                        principalTable: "CAT_rol_comite",
                        principalColumn: "id_rol_comite");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acuerdo_evaluacion",
                columns: table => new
                {
                    id_acuerdo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    fecha_compromiso = table.Column<DateTime>(type: "date", nullable: true),
                    cumplido = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_acuerdo);
                    table.ForeignKey(
                        name: "fk_acuerdo_revision",
                        column: x => x.id_revision,
                        principalTable: "revision",
                        principalColumn: "id_revision");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "documento",
                columns: table => new
                {
                    id_documento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    clave_alumno = table.Column<int>(type: "int", nullable: true),
                    id_tesis = table.Column<int>(type: "int", nullable: true),
                    id_revision = table.Column<int>(type: "int", nullable: true),
                    cve_acta = table.Column<string>(type: "char(6)", fixedLength: true, maxLength: 6, nullable: true),
                    id_tipo_documento = table.Column<int>(type: "int", nullable: true),
                    nombre_archivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ruta_archivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    fecha_carga = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    id_usuario = table.Column<int>(type: "int", nullable: true, comment: "Quién lo subió"),
                    verificado = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "Constancia revisada por el coordinador")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_documento);
                    table.ForeignKey(
                        name: "fk_doc_acta",
                        column: x => x.cve_acta,
                        principalTable: "acta_comite",
                        principalColumn: "cve_acta");
                    table.ForeignKey(
                        name: "fk_doc_alumno",
                        column: x => x.clave_alumno,
                        principalTable: "alumno",
                        principalColumn: "clave_alumno");
                    table.ForeignKey(
                        name: "fk_doc_revision",
                        column: x => x.id_revision,
                        principalTable: "revision",
                        principalColumn: "id_revision");
                    table.ForeignKey(
                        name: "fk_doc_tesis",
                        column: x => x.id_tesis,
                        principalTable: "tesis",
                        principalColumn: "id_tesis");
                    table.ForeignKey(
                        name: "fk_doc_tipo",
                        column: x => x.id_tipo_documento,
                        principalTable: "CAT_tipo_documento",
                        principalColumn: "id_tipo_documento");
                    table.ForeignKey(
                        name: "fk_doc_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "revision_profesor",
                columns: table => new
                {
                    id_revision_profesor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    rpe = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    id_revision = table.Column<int>(type: "int", nullable: false),
                    comentarios = table.Column<string>(type: "text", nullable: true),
                    calif_documento = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    calif_exposicion = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    calif_dominio = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    calif_grado_avance = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    porcentaje_avance = table.Column<decimal>(type: "decimal(5,2)", precision: 5, nullable: true),
                    fecha_envio = table.Column<DateTime>(type: "datetime", nullable: true, comment: "Nulo mientras no envía su revisión"),
                    fecha_conformidad = table.Column<DateTime>(type: "datetime", nullable: true, comment: "Su firma del acta")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_revision_profesor);
                    table.CheckConstraint("ck_revprof_calif", "calif_documento <= 100 AND calif_exposicion <= 100 AND calif_dominio <= 100 AND calif_grado_avance <= 100");
                    table.ForeignKey(
                        name: "fk_rp_profesor",
                        column: x => x.rpe,
                        principalTable: "profesor",
                        principalColumn: "rpe");
                    table.ForeignKey(
                        name: "fk_rp_revision",
                        column: x => x.id_revision,
                        principalTable: "revision",
                        principalColumn: "id_revision");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "CAT_estado_academico",
                columns: new[] { "id_estado_academico", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Alumno inscrito y cursando el programa", "Activo" },
                    { 2, "Alumno que concluyó su titulación", "Titulado" },
                    { 3, "Alumno separado del programa de forma permanente", "Baja definitiva" },
                    { 4, "Alumno con suspensión temporal; puede reingresar", "Baja temporal" }
                });

            migrationBuilder.InsertData(
                table: "CAT_estado_egresado",
                columns: new[] { "id_estado_egresado", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Egresado con datos de contacto vigentes", "Localizable" },
                    { 2, "Sin respuesta tras tres intentos de contacto", "No localizable" }
                });

            migrationBuilder.InsertData(
                table: "CAT_estado_tesis",
                columns: new[] { "id_estado_tesis", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Trabajo registrado sin avance", "Sin iniciar" },
                    { 2, "Trabajo en desarrollo", "En progreso" },
                    { 3, "Trabajo en evaluación por el comité tutorial", "En evaluación" },
                    { 4, "Trabajo con resolución final favorable", "Aceptada" },
                    { 5, "Trabajo cerrado por baja definitiva del alumno", "Cancelada" }
                });

            migrationBuilder.InsertData(
                table: "CAT_estado_toefl",
                columns: new[] { "id_estado_toefl", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "El alumno no ha presentado el examen", "Pendiente" },
                    { 2, "Comprobante enviado, pendiente de aprobar", "Presentado" },
                    { 3, "Requisito TOEFL cumplido", "Aprobado" }
                });

            migrationBuilder.InsertData(
                table: "CAT_formato_reporte",
                columns: new[] { "id_formato_reporte", "nombre" },
                values: new object[,]
                {
                    { 1, "PDF" },
                    { 2, "Excel" }
                });

            migrationBuilder.InsertData(
                table: "CAT_modalidad_titulacion",
                columns: new[] { "id_modalidad", "activo", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, true, "Titulación mediante trabajo de tesis", "Tesis" },
                    { 2, true, "Titulación mediante publicación de artículo científico", "Artículo científico" }
                });

            migrationBuilder.InsertData(
                table: "CAT_programa_academico",
                columns: new[] { "id_programa", "activo", "descripcion", "nombre" },
                values: new object[] { 1, true, "Programa de maestría de la Facultad de Ingeniería de la UASLP", "Maestría en Tecnología y Gestión del Agua" });

            migrationBuilder.InsertData(
                table: "CAT_rol_comite",
                columns: new[] { "id_rol_comite", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Dirige el trabajo y arma el comité", "Director" },
                    { 2, "Comparte la dirección del trabajo (opcional)", "Codirector" },
                    { 3, "Integrante del comité tutorial", "Sinodal" }
                });

            migrationBuilder.InsertData(
                table: "CAT_rol_usuario",
                columns: new[] { "id_rol_usuario", "activo", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, true, "Gestiona alumnos, titulación, comités, egresados, reportes y usuarios; también es profesor", "Coordinador" },
                    { 2, true, "Lo mismo que el coordinador, sin la parte de profesor, más el registro de accesos", "Administrador" },
                    { 3, true, "Consulta a los alumnos de sus comités, atiende solicitudes de dirección y evalúa", "Profesor" },
                    { 4, true, "Consulta su titulación, su comité y sus evaluaciones; sube evidencias y su TOEFL", "Alumno" },
                    { 5, true, "Actualiza su perfil, su historial laboral y sus constancias", "Egresado" }
                });

            migrationBuilder.InsertData(
                table: "CAT_situacion_laboral",
                columns: new[] { "id_situacion_laboral", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Tiene un empleo vigente", "Con trabajo" },
                    { 2, "No tiene ningún empleo vigente", "Sin trabajo" },
                    { 3, "Tiene una beca vigente", "Becario" },
                    { 4, "Tiene una actividad vigente de otro tipo", "Otro" }
                });

            migrationBuilder.InsertData(
                table: "CAT_tipo_documento",
                columns: new[] { "id_tipo_documento", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Documento de avance que sube el alumno", "Avance" },
                    { 2, "Evidencia que sube el alumno", "Evidencia" },
                    { 3, "Acta que genera el sistema al cerrar la evaluación", "Acta de evaluación" },
                    { 4, "Formato institucional para descarga", "Formato oficial" },
                    { 5, "Versión final del trabajo de titulación", "Trabajo final" },
                    { 6, "Reporte generado por el sistema", "Reporte" },
                    { 7, "Constancia que sube el egresado", "Constancia" },
                    { 8, "Comprobante del examen TOEFL que sube el alumno", "Comprobante TOEFL" }
                });

            migrationBuilder.InsertData(
                table: "CAT_tipo_reporte",
                columns: new[] { "id_tipo_reporte", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Egresados por generación, estado y situación laboral", "Reporte de egresados" },
                    { 2, "Avance de titulación con rezago por avance y por tiempo", "Reporte de avance de titulación" },
                    { 3, "Tiempo para emplearse y relación del empleo con la maestría", "Reporte de situación laboral" },
                    { 4, "Estado de los alumnos y titulación en tiempo por generación", "Reporte por generación" }
                });

            migrationBuilder.CreateIndex(
                name: "fk_acta_alumno",
                table: "acta_comite",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_acta_comite",
                table: "acta_comite",
                column: "id_comite");

            migrationBuilder.CreateIndex(
                name: "fk_acta_revision",
                table: "acta_comite",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "fk_acta_toefl",
                table: "acta_comite",
                column: "id_toefl");

            migrationBuilder.CreateIndex(
                name: "fk_acuerdo_revision",
                table: "acuerdo_evaluacion",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "fk_alumno_estado_academico",
                table: "alumno",
                column: "id_estado_academico");

            migrationBuilder.CreateIndex(
                name: "fk_alumno_generacion",
                table: "alumno",
                column: "id_generacion");

            migrationBuilder.CreateIndex(
                name: "fk_alumno_modalidad",
                table: "alumno",
                column: "id_modalidad");

            migrationBuilder.CreateIndex(
                name: "fk_alumno_programa",
                table: "alumno",
                column: "id_programa");

            migrationBuilder.CreateIndex(
                name: "fk_alumno_usuario",
                table: "alumno",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "fk_detalle_periodo",
                table: "alumno_detalle",
                column: "id_periodo");

            migrationBuilder.CreateIndex(
                name: "uq_alumno_periodo",
                table: "alumno_detalle",
                columns: new[] { "clave_alumno", "id_periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "clave",
                table: "comite",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_comite_tesis",
                table: "comite",
                column: "id_tesis");

            migrationBuilder.CreateIndex(
                name: "fk_cp_comite",
                table: "comite_profesor",
                column: "id_comite");

            migrationBuilder.CreateIndex(
                name: "fk_cp_profesor",
                table: "comite_profesor",
                column: "rpe");

            migrationBuilder.CreateIndex(
                name: "fk_cp_rol",
                table: "comite_profesor",
                column: "id_rol_comite");

            migrationBuilder.CreateIndex(
                name: "fk_detempleo_empleo",
                table: "detalle_empleo",
                column: "id_empleo");

            migrationBuilder.CreateIndex(
                name: "fk_doc_acta",
                table: "documento",
                column: "cve_acta");

            migrationBuilder.CreateIndex(
                name: "fk_doc_alumno",
                table: "documento",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_doc_revision",
                table: "documento",
                column: "id_revision");

            migrationBuilder.CreateIndex(
                name: "fk_doc_tesis",
                table: "documento",
                column: "id_tesis");

            migrationBuilder.CreateIndex(
                name: "fk_doc_tipo",
                table: "documento",
                column: "id_tipo_documento");

            migrationBuilder.CreateIndex(
                name: "fk_doc_usuario",
                table: "documento",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "fk_egresado_estado",
                table: "egresado",
                column: "id_estado_egresado");

            migrationBuilder.CreateIndex(
                name: "identificador_egresado",
                table: "egresado",
                column: "identificador_egresado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_empleo_egresado",
                table: "empleo",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_empleo_empresa",
                table: "empleo",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "fk_generacion_programa",
                table: "generacion",
                column: "id_programa");

            migrationBuilder.CreateIndex(
                name: "fk_intento_egresado",
                table: "intento_contacto",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_log_usuario",
                table: "log_acceso",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "fk_profdet_profesor",
                table: "profesor_detalle",
                column: "rpe");

            migrationBuilder.CreateIndex(
                name: "fk_profdet_usuario",
                table: "profesor_detalle",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "fk_reporte_formato",
                table: "reporte",
                column: "id_formato_reporte");

            migrationBuilder.CreateIndex(
                name: "fk_reporte_tipo",
                table: "reporte",
                column: "id_tipo_reporte");

            migrationBuilder.CreateIndex(
                name: "fk_reporte_usuario",
                table: "reporte",
                column: "id_usuario_genero");

            migrationBuilder.CreateIndex(
                name: "fk_toefl_alumno",
                table: "requisito_toefl",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_toefl_estado",
                table: "requisito_toefl",
                column: "id_estado_toefl");

            migrationBuilder.CreateIndex(
                name: "fk_retro_egresado",
                table: "retroalimentacion_egresado",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_revision_periodo",
                table: "revision",
                column: "id_periodo");

            migrationBuilder.CreateIndex(
                name: "fk_revision_tesis",
                table: "revision",
                column: "id_tesis");

            migrationBuilder.CreateIndex(
                name: "fk_rp_profesor",
                table: "revision_profesor",
                column: "rpe");

            migrationBuilder.CreateIndex(
                name: "uq_revision_profesor",
                table: "revision_profesor",
                columns: new[] { "id_revision", "rpe" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sesion_usuario",
                table: "sesion_usuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "fk_tesis_acta_resolucion",
                table: "tesis",
                column: "cve_acta_resolucion");

            migrationBuilder.CreateIndex(
                name: "fk_tesis_alumno",
                table: "tesis",
                column: "clave_alumno");

            migrationBuilder.CreateIndex(
                name: "fk_tesis_director",
                table: "tesis",
                column: "rpe_director");

            migrationBuilder.CreateIndex(
                name: "fk_tesis_estado",
                table: "tesis",
                column: "id_estado_tesis");

            migrationBuilder.CreateIndex(
                name: "fk_tesis_modalidad",
                table: "tesis",
                column: "id_modalidad");

            migrationBuilder.CreateIndex(
                name: "fk_tesis_programa",
                table: "tesis",
                column: "id_programa");

            migrationBuilder.CreateIndex(
                name: "ix_tesis_solicitud",
                table: "tesis",
                columns: new[] { "estado_solicitud", "rpe_director" });

            migrationBuilder.CreateIndex(
                name: "fk_token_usuario",
                table: "token_recuperacion",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "token",
                table: "token_recuperacion",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "clave_institucional",
                table: "usuario",
                column: "clave_institucional",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "correo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_usuario_rol",
                table: "usuario",
                column: "id_rol_usuario");

            migrationBuilder.AddForeignKey(
                name: "fk_acta_comite",
                table: "acta_comite",
                column: "id_comite",
                principalTable: "comite",
                principalColumn: "id_comite");

            migrationBuilder.AddForeignKey(
                name: "fk_acta_revision",
                table: "acta_comite",
                column: "id_revision",
                principalTable: "revision",
                principalColumn: "id_revision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_acta_alumno",
                table: "acta_comite");

            migrationBuilder.DropForeignKey(
                name: "fk_toefl_alumno",
                table: "requisito_toefl");

            migrationBuilder.DropForeignKey(
                name: "fk_tesis_alumno",
                table: "tesis");

            migrationBuilder.DropForeignKey(
                name: "fk_acta_comite",
                table: "acta_comite");

            migrationBuilder.DropForeignKey(
                name: "fk_acta_revision",
                table: "acta_comite");

            migrationBuilder.DropTable(
                name: "acuerdo_evaluacion");

            migrationBuilder.DropTable(
                name: "alumno_detalle");

            migrationBuilder.DropTable(
                name: "CAT_situacion_laboral");

            migrationBuilder.DropTable(
                name: "comite_profesor");

            migrationBuilder.DropTable(
                name: "detalle_empleo");

            migrationBuilder.DropTable(
                name: "documento");

            migrationBuilder.DropTable(
                name: "intento_contacto");

            migrationBuilder.DropTable(
                name: "log_acceso");

            migrationBuilder.DropTable(
                name: "profesor_detalle");

            migrationBuilder.DropTable(
                name: "reporte");

            migrationBuilder.DropTable(
                name: "retroalimentacion_egresado");

            migrationBuilder.DropTable(
                name: "revision_profesor");

            migrationBuilder.DropTable(
                name: "sesion_usuario");

            migrationBuilder.DropTable(
                name: "token_recuperacion");

            migrationBuilder.DropTable(
                name: "CAT_rol_comite");

            migrationBuilder.DropTable(
                name: "empleo");

            migrationBuilder.DropTable(
                name: "CAT_tipo_documento");

            migrationBuilder.DropTable(
                name: "CAT_formato_reporte");

            migrationBuilder.DropTable(
                name: "CAT_tipo_reporte");

            migrationBuilder.DropTable(
                name: "egresado");

            migrationBuilder.DropTable(
                name: "empresa");

            migrationBuilder.DropTable(
                name: "CAT_estado_egresado");

            migrationBuilder.DropTable(
                name: "alumno");

            migrationBuilder.DropTable(
                name: "CAT_estado_academico");

            migrationBuilder.DropTable(
                name: "generacion");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "CAT_rol_usuario");

            migrationBuilder.DropTable(
                name: "comite");

            migrationBuilder.DropTable(
                name: "revision");

            migrationBuilder.DropTable(
                name: "periodo_academico");

            migrationBuilder.DropTable(
                name: "tesis");

            migrationBuilder.DropTable(
                name: "acta_comite");

            migrationBuilder.DropTable(
                name: "profesor");

            migrationBuilder.DropTable(
                name: "CAT_estado_tesis");

            migrationBuilder.DropTable(
                name: "CAT_modalidad_titulacion");

            migrationBuilder.DropTable(
                name: "CAT_programa_academico");

            migrationBuilder.DropTable(
                name: "requisito_toefl");

            migrationBuilder.DropTable(
                name: "CAT_estado_toefl");
        }
    }
}
