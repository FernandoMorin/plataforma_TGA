-- =============================================================
-- Plataforma TGA — esquema y catálogos
-- MySQL 9 / InnoDB / utf8mb4
-- Fuente: PlanB/Extras/BASE_DE_DATOS.md
-- Se ejecuta sobre una base vacía, como root:
--   mysql -u root -p < database/esquema.sql
-- =============================================================

CREATE DATABASE IF NOT EXISTS plataforma_TGA
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE plataforma_TGA;
SET NAMES utf8mb4;

-- tesis.cve_acta_resolucion y acta_comite se referencian en círculo:
-- con las revisiones apagadas, MySQL deja crear la FK antes que la tabla.
SET FOREIGN_KEY_CHECKS = 0;

-- =============================================================
-- CATÁLOGOS
-- =============================================================

CREATE TABLE CAT_programa_academico (
  id_programa   INT AUTO_INCREMENT PRIMARY KEY,
  nombre        VARCHAR(150) NOT NULL,
  descripcion   VARCHAR(255),
  activo        BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE CAT_rol_usuario (
  id_rol_usuario INT AUTO_INCREMENT PRIMARY KEY,
  nombre         VARCHAR(100) NOT NULL,
  descripcion    VARCHAR(255),
  activo         BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE CAT_modalidad_titulacion (
  id_modalidad INT AUTO_INCREMENT PRIMARY KEY,
  nombre       VARCHAR(100) NOT NULL,
  descripcion  VARCHAR(255),
  activo       BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE CAT_estado_academico (
  id_estado_academico INT AUTO_INCREMENT PRIMARY KEY,
  nombre              VARCHAR(100) NOT NULL,
  descripcion         VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_estado_tesis (
  id_estado_tesis INT AUTO_INCREMENT PRIMARY KEY,
  nombre          VARCHAR(100) NOT NULL,
  descripcion     VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_estado_toefl (
  id_estado_toefl INT AUTO_INCREMENT PRIMARY KEY,
  nombre          VARCHAR(100) NOT NULL,
  descripcion     VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_rol_comite (
  id_rol_comite INT AUTO_INCREMENT PRIMARY KEY,
  nombre        VARCHAR(100) NOT NULL,
  descripcion   VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_tipo_documento (
  id_tipo_documento INT AUTO_INCREMENT PRIMARY KEY,
  nombre            VARCHAR(100) NOT NULL,
  descripcion       VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_estado_egresado (
  id_estado_egresado INT AUTO_INCREMENT PRIMARY KEY,
  nombre             VARCHAR(100) NOT NULL,
  descripcion        VARCHAR(255)
) ENGINE=InnoDB;

-- Ninguna tabla lo referencia: la situación se calcula a partir de los empleos.
CREATE TABLE CAT_situacion_laboral (
  id_situacion_laboral INT AUTO_INCREMENT PRIMARY KEY,
  nombre               VARCHAR(100) NOT NULL,
  descripcion          VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_tipo_reporte (
  id_tipo_reporte INT AUTO_INCREMENT PRIMARY KEY,
  nombre          VARCHAR(100) NOT NULL,
  descripcion     VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE CAT_formato_reporte (
  id_formato_reporte INT AUTO_INCREMENT PRIMARY KEY,
  nombre             VARCHAR(50) NOT NULL
) ENGINE=InnoDB;

-- =============================================================
-- ACCESO Y SEGURIDAD
-- =============================================================

CREATE TABLE usuario (
  id_usuario          INT AUTO_INCREMENT PRIMARY KEY,
  correo              VARCHAR(100) NOT NULL UNIQUE,
  contrasena_hash     VARCHAR(255) NOT NULL,
  id_rol_usuario      INT,
  activo              BOOLEAN DEFAULT TRUE,
  intentos_fallidos   INT DEFAULT 0,
  bloqueado_hasta     DATETIME,
  requiere_cambio     BOOLEAN DEFAULT FALSE,
  fecha_creacion      DATETIME DEFAULT CURRENT_TIMESTAMP,
  ultimo_acceso       DATETIME,
  clave_institucional VARCHAR(30) UNIQUE COMMENT 'RPE, clave de alumno, EXT-… o identificador de egresado',
  nombre              VARCHAR(150),
  CONSTRAINT fk_usuario_rol
    FOREIGN KEY (id_rol_usuario) REFERENCES CAT_rol_usuario(id_rol_usuario)
) ENGINE=InnoDB;

CREATE TABLE token_recuperacion (
  id_token         INT AUTO_INCREMENT PRIMARY KEY,
  id_usuario       INT NOT NULL,
  token            VARCHAR(255) NOT NULL UNIQUE COMMENT 'Hash del token, nunca el token',
  fecha_expiracion DATETIME NOT NULL,
  usado            BOOLEAN DEFAULT FALSE,
  CONSTRAINT fk_token_usuario
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario)
) ENGINE=InnoDB;

CREATE TABLE sesion_usuario (
  Id              VARCHAR(64) PRIMARY KEY,
  UsuarioId       INT NOT NULL,
  UltimaActividad DATETIME(6) NOT NULL,
  Creada          DATETIME(6) NOT NULL,
  INDEX ix_sesion_usuario (UsuarioId),
  CONSTRAINT fk_sesion_usuario
    FOREIGN KEY (UsuarioId) REFERENCES usuario(id_usuario)
) ENGINE=InnoDB;

-- VARCHAR(30): «Contraseña incorrecta» tiene 21 caracteres y no cabía en el VARCHAR(20) anterior.
CREATE TABLE log_acceso (
  id_log     INT AUTO_INCREMENT PRIMARY KEY,
  id_usuario INT,
  fecha      DATETIME DEFAULT CURRENT_TIMESTAMP,
  resultado  VARCHAR(30) COMMENT 'Correcto | Contraseña incorrecta | Bloqueado | Primer acceso',
  ip         VARCHAR(45),
  CONSTRAINT fk_log_usuario
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario)
) ENGINE=InnoDB;

-- =============================================================
-- ESTRUCTURA ACADÉMICA
-- =============================================================

CREATE TABLE generacion (
  id_generacion        INT AUTO_INCREMENT PRIMARY KEY,
  id_programa          INT NOT NULL,
  anio_ingreso         INT,
  anio_egreso_estimado INT,
  nombre_generacion    VARCHAR(100),
  CONSTRAINT fk_generacion_programa
    FOREIGN KEY (id_programa) REFERENCES CAT_programa_academico(id_programa)
) ENGINE=InnoDB;

CREATE TABLE periodo_academico (
  id_periodo   INT AUTO_INCREMENT PRIMARY KEY,
  nombre       VARCHAR(100),
  fecha_inicio DATE,
  fecha_fin    DATE,
  tipo_periodo VARCHAR(50)
) ENGINE=InnoDB;

-- =============================================================
-- ALUMNO
-- =============================================================

CREATE TABLE alumno (
  clave_alumno         INT PRIMARY KEY COMMENT 'La asigna la universidad',
  nombre               VARCHAR(50),
  apellido_paterno     VARCHAR(50),
  apellido_materno     VARCHAR(50),
  fecha_ingreso        DATE,
  id_usuario           INT,
  id_programa          INT,
  id_generacion        INT,
  correo_institucional VARCHAR(100),
  correo_personal      VARCHAR(100),
  telefono             VARCHAR(20),
  id_modalidad         INT COMMENT 'Nulo = Sin asignar',
  id_estado_academico  INT,
  pendiente_conciliar  BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Alumno temporal: aún no está en el registro de la universidad',
  CONSTRAINT fk_alumno_usuario
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario),
  CONSTRAINT fk_alumno_programa
    FOREIGN KEY (id_programa) REFERENCES CAT_programa_academico(id_programa),
  CONSTRAINT fk_alumno_generacion
    FOREIGN KEY (id_generacion) REFERENCES generacion(id_generacion),
  CONSTRAINT fk_alumno_modalidad
    FOREIGN KEY (id_modalidad) REFERENCES CAT_modalidad_titulacion(id_modalidad),
  CONSTRAINT fk_alumno_estado_academico
    FOREIGN KEY (id_estado_academico) REFERENCES CAT_estado_academico(id_estado_academico)
) ENGINE=InnoDB;

CREATE TABLE alumno_detalle (
  id_alumno_detalle INT AUTO_INCREMENT PRIMARY KEY,
  clave_alumno      INT NOT NULL,
  id_periodo        INT NOT NULL,
  semestre          INT,
  fecha_registro    DATE,
  CONSTRAINT fk_detalle_alumno
    FOREIGN KEY (clave_alumno) REFERENCES alumno(clave_alumno),
  CONSTRAINT fk_detalle_periodo
    FOREIGN KEY (id_periodo) REFERENCES periodo_academico(id_periodo),
  CONSTRAINT uq_alumno_periodo UNIQUE (clave_alumno, id_periodo)
) ENGINE=InnoDB;

-- =============================================================
-- PROFESOR
-- =============================================================

CREATE TABLE profesor (
  rpe              VARCHAR(10) PRIMARY KEY COMMENT 'RPE de la universidad o EXT-NNN para externos',
  nombre           VARCHAR(50),
  apellido_paterno VARCHAR(50),
  apellido_materno VARCHAR(50)
) ENGINE=InnoDB;

CREATE TABLE profesor_detalle (
  id_profesor_detalle  INT AUTO_INCREMENT PRIMARY KEY,
  rpe                  VARCHAR(10) NOT NULL,
  id_usuario           INT,
  correo_institucional VARCHAR(100),
  activo               BOOLEAN DEFAULT TRUE,
  externo              BOOLEAN NOT NULL DEFAULT FALSE,
  institucion          VARCHAR(150) COMMENT 'Solo externos',
  vigente_hasta        DATE COMMENT 'Solo externos: al vencer se desactiva',
  CONSTRAINT fk_profdet_profesor
    FOREIGN KEY (rpe) REFERENCES profesor(rpe),
  CONSTRAINT fk_profdet_usuario
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario)
) ENGINE=InnoDB;

-- =============================================================
-- TRABAJO DE TITULACIÓN (cualquier modalidad)
-- =============================================================

CREATE TABLE tesis (
  id_tesis             INT AUTO_INCREMENT PRIMARY KEY,
  titulo               VARCHAR(255),
  clave_alumno         INT NOT NULL,
  id_programa          INT,
  porcentaje_actual    DECIMAL(5,2),
  id_estado_tesis      INT,
  fecha_registro       DATE,
  linea_investigacion  VARCHAR(255),
  id_modalidad         INT COMMENT 'Queda fijada al aceptarse la solicitud',
  estado_solicitud     VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
  rpe_director         VARCHAR(10),
  fecha_solicitud      DATE,
  fecha_resolucion     DATE COMMENT 'Cuándo aceptó o rechazó el director',
  motivo_rechazo       VARCHAR(255),
  ultima_actualizacion DATETIME,
  fecha_cierre         DATE COMMENT 'Resolución final, la registra el director',
  cve_acta_resolucion  CHAR(6),
  CONSTRAINT ck_tesis_solicitud
    CHECK (estado_solicitud IN ('Pendiente', 'Aceptada', 'Rechazada')),
  CONSTRAINT fk_tesis_alumno
    FOREIGN KEY (clave_alumno) REFERENCES alumno(clave_alumno),
  CONSTRAINT fk_tesis_programa
    FOREIGN KEY (id_programa) REFERENCES CAT_programa_academico(id_programa),
  CONSTRAINT fk_tesis_estado
    FOREIGN KEY (id_estado_tesis) REFERENCES CAT_estado_tesis(id_estado_tesis),
  CONSTRAINT fk_tesis_modalidad
    FOREIGN KEY (id_modalidad) REFERENCES CAT_modalidad_titulacion(id_modalidad),
  CONSTRAINT fk_tesis_director
    FOREIGN KEY (rpe_director) REFERENCES profesor(rpe),
  CONSTRAINT fk_tesis_acta_resolucion
    FOREIGN KEY (cve_acta_resolucion) REFERENCES acta_comite(cve_acta),
  KEY ix_tesis_solicitud (estado_solicitud, rpe_director)
) ENGINE=InnoDB;

-- =============================================================
-- COMITÉ TUTORIAL
-- =============================================================

CREATE TABLE comite (
  id_comite INT AUTO_INCREMENT PRIMARY KEY,
  id_tesis  INT NOT NULL,
  clave     VARCHAR(50) NOT NULL UNIQUE COMMENT 'CT-AAAA-NNNN, la genera el servidor',
  CONSTRAINT fk_comite_tesis
    FOREIGN KEY (id_tesis) REFERENCES tesis(id_tesis)
) ENGINE=InnoDB;

-- Una fila por cada vez que un profesor entra al comité; el historial sale de aquí.
CREATE TABLE comite_profesor (
  id_comite_profesor INT AUTO_INCREMENT PRIMARY KEY,
  id_comite          INT NOT NULL,
  rpe                VARCHAR(10) NOT NULL,
  id_rol_comite      INT NOT NULL,
  fecha_asignacion   DATE,
  activo             BOOLEAN DEFAULT TRUE,
  fecha_baja         DATE,
  CONSTRAINT fk_cp_comite
    FOREIGN KEY (id_comite) REFERENCES comite(id_comite),
  CONSTRAINT fk_cp_profesor
    FOREIGN KEY (rpe) REFERENCES profesor(rpe),
  CONSTRAINT fk_cp_rol
    FOREIGN KEY (id_rol_comite) REFERENCES CAT_rol_comite(id_rol_comite)
) ENGINE=InnoDB;

-- =============================================================
-- REQUISITO TOEFL
-- =============================================================

CREATE TABLE requisito_toefl (
  id_toefl           INT AUTO_INCREMENT PRIMARY KEY,
  clave_alumno       INT NOT NULL,
  id_estado_toefl    INT,
  fecha_presentacion DATE COMMENT 'La registra el alumno',
  puntaje            DECIMAL(5,2) COMMENT 'Lo captura el coordinador al aprobar',
  observaciones      TEXT,
  fecha_proxima      DATE COMMENT 'Próxima fecha para presentar; la registra el alumno',
  CONSTRAINT fk_toefl_alumno
    FOREIGN KEY (clave_alumno) REFERENCES alumno(clave_alumno),
  CONSTRAINT fk_toefl_estado
    FOREIGN KEY (id_estado_toefl) REFERENCES CAT_estado_toefl(id_estado_toefl)
) ENGINE=InnoDB;

-- =============================================================
-- EVALUACIONES (sobre el acta oficial del comité tutorial)
-- =============================================================

-- Evaluación final que consolida el director. Una por informe, sin tope.
CREATE TABLE revision (
  id_revision        INT AUTO_INCREMENT PRIMARY KEY,
  fecha              DATE,
  id_tesis           INT NOT NULL,
  id_periodo         INT,
  comentarios        TEXT,
  porcentaje_avance  DECIMAL(5,2),
  estado             VARCHAR(50) NOT NULL DEFAULT 'Abierta',
  calif_documento    TINYINT UNSIGNED,
  calif_exposicion   TINYINT UNSIGNED,
  calif_dominio      TINYINT UNSIGNED,
  calif_grado_avance TINYINT UNSIGNED,
  CONSTRAINT ck_revision_estado
    CHECK (estado IN ('Abierta', 'En consolidación', 'En firma', 'Cerrada')),
  CONSTRAINT ck_revision_calif CHECK (
    calif_documento <= 100 AND calif_exposicion <= 100 AND
    calif_dominio <= 100 AND calif_grado_avance <= 100),
  CONSTRAINT fk_revision_tesis
    FOREIGN KEY (id_tesis) REFERENCES tesis(id_tesis),
  CONSTRAINT fk_revision_periodo
    FOREIGN KEY (id_periodo) REFERENCES periodo_academico(id_periodo)
) ENGINE=InnoDB;

-- Revisión individual de cada integrante del comité.
CREATE TABLE revision_profesor (
  id_revision_profesor INT AUTO_INCREMENT PRIMARY KEY,
  rpe                  VARCHAR(10) NOT NULL,
  id_revision          INT NOT NULL,
  comentarios          TEXT,
  calif_documento      TINYINT UNSIGNED,
  calif_exposicion     TINYINT UNSIGNED,
  calif_dominio        TINYINT UNSIGNED,
  calif_grado_avance   TINYINT UNSIGNED,
  porcentaje_avance    DECIMAL(5,2),
  fecha_envio          DATETIME COMMENT 'Nulo mientras no envía su revisión',
  fecha_conformidad    DATETIME COMMENT 'Su firma del acta',
  CONSTRAINT uq_revision_profesor UNIQUE (id_revision, rpe),
  CONSTRAINT ck_revprof_calif CHECK (
    calif_documento <= 100 AND calif_exposicion <= 100 AND
    calif_dominio <= 100 AND calif_grado_avance <= 100),
  CONSTRAINT fk_rp_profesor
    FOREIGN KEY (rpe) REFERENCES profesor(rpe),
  CONSTRAINT fk_rp_revision
    FOREIGN KEY (id_revision) REFERENCES revision(id_revision)
) ENGINE=InnoDB;

CREATE TABLE acuerdo_evaluacion (
  id_acuerdo       INT AUTO_INCREMENT PRIMARY KEY,
  id_revision      INT NOT NULL,
  descripcion      TEXT,
  fecha_compromiso DATE,
  cumplido         BOOLEAN DEFAULT FALSE,
  CONSTRAINT fk_acuerdo_revision
    FOREIGN KEY (id_revision) REFERENCES revision(id_revision)
) ENGINE=InnoDB;

CREATE TABLE acta_comite (
  cve_acta       CHAR(6) PRIMARY KEY,
  clave_alumno   INT NOT NULL,
  id_comite      INT NOT NULL,
  id_revision    INT,
  id_toefl       INT,
  num_evaluacion INT,
  fecha_reunion  DATE,
  observaciones  TEXT,
  resultado      VARCHAR(50),
  archivo_acta   VARCHAR(255),
  estado         BOOLEAN DEFAULT TRUE,
  CONSTRAINT fk_acta_alumno
    FOREIGN KEY (clave_alumno) REFERENCES alumno(clave_alumno),
  CONSTRAINT fk_acta_comite
    FOREIGN KEY (id_comite) REFERENCES comite(id_comite),
  CONSTRAINT fk_acta_revision
    FOREIGN KEY (id_revision) REFERENCES revision(id_revision),
  CONSTRAINT fk_acta_toefl
    FOREIGN KEY (id_toefl) REFERENCES requisito_toefl(id_toefl)
) ENGINE=InnoDB;

-- =============================================================
-- DOCUMENTOS
-- =============================================================

CREATE TABLE documento (
  id_documento      INT AUTO_INCREMENT PRIMARY KEY,
  clave_alumno      INT,
  id_tesis          INT,
  id_revision       INT,
  cve_acta          CHAR(6),
  id_tipo_documento INT,
  nombre_archivo    VARCHAR(255),
  ruta_archivo      VARCHAR(255),
  fecha_carga       DATETIME DEFAULT CURRENT_TIMESTAMP,
  id_usuario        INT COMMENT 'Quién lo subió',
  verificado        BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Constancia revisada por el coordinador',
  CONSTRAINT fk_doc_alumno
    FOREIGN KEY (clave_alumno) REFERENCES alumno(clave_alumno),
  CONSTRAINT fk_doc_tesis
    FOREIGN KEY (id_tesis) REFERENCES tesis(id_tesis),
  CONSTRAINT fk_doc_revision
    FOREIGN KEY (id_revision) REFERENCES revision(id_revision),
  CONSTRAINT fk_doc_acta
    FOREIGN KEY (cve_acta) REFERENCES acta_comite(cve_acta),
  CONSTRAINT fk_doc_tipo
    FOREIGN KEY (id_tipo_documento) REFERENCES CAT_tipo_documento(id_tipo_documento),
  CONSTRAINT fk_doc_usuario
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario)
) ENGINE=InnoDB;

-- =============================================================
-- EGRESADO Y EMPLEO
-- =============================================================

CREATE TABLE egresado (
  clave_alumno           INT PRIMARY KEY,
  identificador_egresado VARCHAR(50) UNIQUE COMMENT 'Generación + iniciales + consecutivo, p. ej. 2022RSC01',
  fecha_egreso           DATE,
  id_estado_egresado     INT,
  correo                 VARCHAR(100) COMMENT 'Correo personal = su usuario',
  telefono               VARCHAR(20),
  ciudad                 VARCHAR(100),
  otros_contactos        VARCHAR(255),
  ultima_actualizacion   DATE,
  CONSTRAINT fk_egresado_alumno
    FOREIGN KEY (clave_alumno) REFERENCES alumno(clave_alumno),
  CONSTRAINT fk_egresado_estado
    FOREIGN KEY (id_estado_egresado) REFERENCES CAT_estado_egresado(id_estado_egresado)
) ENGINE=InnoDB;

CREATE TABLE empresa (
  id_empresa INT AUTO_INCREMENT PRIMARY KEY,
  nombre     VARCHAR(150),
  sector     VARCHAR(100),
  pais       VARCHAR(100),
  estado     VARCHAR(100),
  ciudad     VARCHAR(100)
) ENGINE=InnoDB;

CREATE TABLE empleo (
  id_empleo        INT AUTO_INCREMENT PRIMARY KEY,
  clave_alumno     INT NOT NULL,
  id_empresa       INT,
  tipo             VARCHAR(10) NOT NULL,
  area_profesional VARCHAR(100),
  CONSTRAINT ck_empleo_tipo CHECK (tipo IN ('Empleo', 'Beca', 'Otro')),
  CONSTRAINT fk_empleo_egresado
    FOREIGN KEY (clave_alumno) REFERENCES egresado(clave_alumno),
  CONSTRAINT fk_empleo_empresa
    FOREIGN KEY (id_empresa) REFERENCES empresa(id_empresa)
) ENGINE=InnoDB;

CREATE TABLE detalle_empleo (
  id_detalle_empleo INT AUTO_INCREMENT PRIMARY KEY,
  id_empleo         INT NOT NULL,
  puesto            VARCHAR(100),
  fecha_inicio      DATE,
  fecha_final       DATE COMMENT 'Nula o futura = empleo vigente',
  sueldo            DECIMAL(10,2),
  relacion_posgrado BOOLEAN,
  actividades       TEXT,
  CONSTRAINT fk_detempleo_empleo
    FOREIGN KEY (id_empleo) REFERENCES empleo(id_empleo)
) ENGINE=InnoDB;

CREATE TABLE intento_contacto (
  id_intento    INT AUTO_INCREMENT PRIMARY KEY,
  clave_alumno  INT NOT NULL,
  fecha_intento DATE,
  observaciones TEXT,
  CONSTRAINT fk_intento_egresado
    FOREIGN KEY (clave_alumno) REFERENCES egresado(clave_alumno)
) ENGINE=InnoDB;

-- En la interfaz se llama «Comentarios».
CREATE TABLE retroalimentacion_egresado (
  id_retroalimentacion INT AUTO_INCREMENT PRIMARY KEY,
  clave_alumno         INT NOT NULL,
  fecha                DATE,
  comentario           TEXT,
  sugerencia           TEXT,
  CONSTRAINT fk_retro_egresado
    FOREIGN KEY (clave_alumno) REFERENCES egresado(clave_alumno)
) ENGINE=InnoDB;

-- =============================================================
-- REPORTES
-- =============================================================

CREATE TABLE reporte (
  id_reporte         INT AUTO_INCREMENT PRIMARY KEY,
  id_tipo_reporte    INT,
  id_formato_reporte INT,
  id_usuario_genero  INT,
  fecha_generacion   DATETIME DEFAULT CURRENT_TIMESTAMP,
  filtros_usados     TEXT,
  ruta_archivo       VARCHAR(255),
  CONSTRAINT fk_reporte_tipo
    FOREIGN KEY (id_tipo_reporte) REFERENCES CAT_tipo_reporte(id_tipo_reporte),
  CONSTRAINT fk_reporte_formato
    FOREIGN KEY (id_formato_reporte) REFERENCES CAT_formato_reporte(id_formato_reporte),
  CONSTRAINT fk_reporte_usuario
    FOREIGN KEY (id_usuario_genero) REFERENCES usuario(id_usuario)
) ENGINE=InnoDB;

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================
-- SEED DE CATÁLOGOS
-- =============================================================

INSERT INTO CAT_programa_academico (id_programa, nombre, descripcion) VALUES
  (1, 'Maestría en Tecnología y Gestión del Agua', 'Programa de maestría de la Facultad de Ingeniería de la UASLP');

-- El profesor externo usa el rol Profesor y se distingue por profesor_detalle.externo.
INSERT INTO CAT_rol_usuario (id_rol_usuario, nombre, descripcion) VALUES
  (1, 'Coordinador',   'Gestiona alumnos, titulación, comités, egresados, reportes y usuarios; también es profesor'),
  (2, 'Administrador', 'Lo mismo que el coordinador, sin la parte de profesor, más el registro de accesos'),
  (3, 'Profesor',      'Consulta a los alumnos de sus comités, atiende solicitudes de dirección y evalúa'),
  (4, 'Alumno',        'Consulta su titulación, su comité y sus evaluaciones; sube evidencias y su TOEFL'),
  (5, 'Egresado',      'Actualiza su perfil, su historial laboral y sus constancias');

INSERT INTO CAT_modalidad_titulacion (id_modalidad, nombre, descripcion) VALUES
  (1, 'Tesis',               'Titulación mediante trabajo de tesis'),
  (2, 'Artículo científico', 'Titulación mediante publicación de artículo científico');

INSERT INTO CAT_estado_academico (id_estado_academico, nombre, descripcion) VALUES
  (1, 'Activo',          'Alumno inscrito y cursando el programa'),
  (2, 'Titulado',        'Alumno que concluyó su titulación'),
  (3, 'Baja definitiva', 'Alumno separado del programa de forma permanente'),
  (4, 'Baja temporal',   'Alumno con suspensión temporal; puede reingresar');

INSERT INTO CAT_estado_tesis (id_estado_tesis, nombre, descripcion) VALUES
  (1, 'Sin iniciar',   'Trabajo registrado sin avance'),
  (2, 'En progreso',   'Trabajo en desarrollo'),
  (3, 'En evaluación', 'Trabajo en evaluación por el comité tutorial'),
  (4, 'Aceptada',      'Trabajo con resolución final favorable'),
  (5, 'Cancelada',     'Trabajo cerrado por baja definitiva del alumno');

INSERT INTO CAT_estado_toefl (id_estado_toefl, nombre, descripcion) VALUES
  (1, 'Pendiente',  'El alumno no ha presentado el examen'),
  (2, 'Presentado', 'Comprobante enviado, pendiente de aprobar'),
  (3, 'Aprobado',   'Requisito TOEFL cumplido');

INSERT INTO CAT_rol_comite (id_rol_comite, nombre, descripcion) VALUES
  (1, 'Director',   'Dirige el trabajo y arma el comité'),
  (2, 'Codirector', 'Comparte la dirección del trabajo (opcional)'),
  (3, 'Sinodal',    'Integrante del comité tutorial');

INSERT INTO CAT_tipo_documento (id_tipo_documento, nombre, descripcion) VALUES
  (1, 'Avance',             'Documento de avance que sube el alumno'),
  (2, 'Evidencia',          'Evidencia que sube el alumno'),
  (3, 'Acta de evaluación', 'Acta que genera el sistema al cerrar la evaluación'),
  (4, 'Formato oficial',    'Formato institucional para descarga'),
  (5, 'Trabajo final',      'Versión final del trabajo de titulación'),
  (6, 'Reporte',            'Reporte generado por el sistema'),
  (7, 'Constancia',         'Constancia que sube el egresado'),
  (8, 'Comprobante TOEFL',  'Comprobante del examen TOEFL que sube el alumno');

INSERT INTO CAT_estado_egresado (id_estado_egresado, nombre, descripcion) VALUES
  (1, 'Localizable',    'Egresado con datos de contacto vigentes'),
  (2, 'No localizable', 'Sin respuesta tras tres intentos de contacto');

INSERT INTO CAT_situacion_laboral (id_situacion_laboral, nombre, descripcion) VALUES
  (1, 'Con trabajo', 'Tiene un empleo vigente'),
  (2, 'Sin trabajo', 'No tiene ningún empleo vigente'),
  (3, 'Becario',     'Tiene una beca vigente'),
  (4, 'Otro',        'Tiene una actividad vigente de otro tipo');

INSERT INTO CAT_tipo_reporte (id_tipo_reporte, nombre, descripcion) VALUES
  (1, 'Reporte de egresados',             'Egresados por generación, estado y situación laboral'),
  (2, 'Reporte de avance de titulación',  'Avance de titulación con rezago por avance y por tiempo'),
  (3, 'Reporte de situación laboral',     'Tiempo para emplearse y relación del empleo con la maestría'),
  (4, 'Reporte por generación',           'Estado de los alumnos y titulación en tiempo por generación');

INSERT INTO CAT_formato_reporte (id_formato_reporte, nombre) VALUES
  (1, 'PDF'),
  (2, 'Excel');
