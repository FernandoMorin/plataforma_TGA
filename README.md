# Plataforma TGA

Seguimiento de alumnos, titulación y egresados de la Maestría en Tecnología y Gestión del Agua
(Facultad de Ingeniería, UASLP).

| Carpeta | Qué es |
|---|---|
| `backend/` | API en ASP.NET Core (.NET 10, EF Core 10). La base de datos se define aquí, con migraciones |
| `frontend/` | Interfaz en Next.js 16 (TypeScript y Tailwind) |
| `database/` | `temporal.sql`: simula el registro de la universidad para importar alumnos (solo desarrollo) |

El navegador solo habla con Next.js; las peticiones a `/api/*` las reenvía Next.js al backend.

## Requisitos

| Herramienta | Versión | Comprobar |
|---|---|---|
| .NET SDK | 10 | `dotnet --list-sdks` |
| Node.js | 22 o superior | `node -v` |
| MySQL | 9 | `mysql --version` |

## 1. Herramientas del proyecto

Desde esta carpeta (`Plataforma-TGA/`), instala `dotnet-ef` en la versión que fija el proyecto:

```bash
dotnet tool restore
```

## 2. Base de datos

Todos los comandos de esta sección se corren desde `Plataforma-TGA/`.

### 2.1 Crear la base y el usuario de la aplicación

Como root de MySQL, una sola vez. Si el usuario `admin` ya existe, omite el `CREATE USER`:

```sql
CREATE DATABASE plataforma_TGA CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'admin'@'localhost' IDENTIFIED BY '<contraseña>';
GRANT SELECT, INSERT, UPDATE, DELETE ON plataforma_TGA.* TO 'admin'@'localhost';
```

> En Linux, MySQL distingue mayúsculas en el nombre de la base: siempre es `plataforma_TGA`.

### 2.2 Guardar la cadena de conexión

No va en el repositorio: se guarda con `dotnet user-secrets`, una vez por máquina:

```bash
dotnet user-secrets set "ConnectionStrings:TgaDb" "Server=localhost;Port=3306;Database=plataforma_TGA;User=admin;Password=<contraseña>;" --project backend
```

### 2.3 Crear las tablas (aplicar las migraciones)

El usuario `admin` solo lee y escribe datos, así que las migraciones no se aplican con
`dotnet ef database update`: se genera el SQL y se corre como root.

```bash
dotnet dotnet-ef migrations script --project backend -o migraciones.sql
mysql -u root -p plataforma_TGA < migraciones.sql
rm migraciones.sql
```

Esto crea todas las tablas y llena los catálogos (roles, estados, tipos de documento...). En
MySQL Workbench: conéctate como root, selecciona `plataforma_TGA`, abre `migraciones.sql` y
ejecútalo con el botón del rayo (Execute).

### 2.4 Cuando llegan migraciones nuevas

Revisa cuáles faltan en tu base (salen marcadas como `Pending`):

```bash
dotnet dotnet-ef migrations list --project backend
```

Genera el SQL desde la **última que ya tienes aplicada** y córrelo como root:

```bash
dotnet dotnet-ef migrations script <ÚltimaAplicada> --project backend -o migraciones.sql
mysql -u root -p plataforma_TGA < migraciones.sql
rm migraciones.sql
```

> No uses `--idempotent`: con el proveedor de MySQL genera SQL que MySQL no acepta.

### 2.5 Cambiar la base de datos

Nadie toca la base a mano: los cambios se hacen en el código y se convierten en una migración.

1. Cambia la entidad en `backend/Models/` o su configuración en
   `backend/Data/TgaDbContext.Configuracion.cs`.
2. Crea la migración:

   ```bash
   dotnet dotnet-ef migrations add <NombreDelCambio> --project backend --output-dir Data/Migraciones
   ```

3. Revisa el SQL y aplícalo como en el paso 2.4.

### 2.6 Registro de la universidad (para importar alumnos)

Los alumnos no se dan de alta a mano: se importan del registro oficial de la universidad, del que
la Plataforma **solo lee**. En desarrollo ese registro se simula con el esquema `TEMPORAL`
(15 estudiantes de ejemplo), al que solo puede leer el usuario `gas_lectura`.

1. Abre `database/temporal.sql`, cambia `<contraseña>` en el `CREATE USER` y córrelo como root.
   El script borra y vuelve a crear el esquema y el usuario, así que se puede correr las veces que
   haga falta:

   ```bash
   mysql -u root -p < database/temporal.sql
   ```

2. Guarda la cadena de conexión con esa contraseña:

   ```bash
   dotnet user-secrets set "ConnectionStrings:UniversidadDb" "Server=localhost;Port=3306;Database=TEMPORAL;User=gas_lectura;Password=<contraseña>;" --project backend
   ```

> El usuario `admin` de la aplicación no tiene ningún permiso sobre `TEMPORAL`, y `gas_lectura`
> solo puede leer la tabla `estudiante`. Al implantar, se cambia esta cadena de conexión para que
> apunte a la base real de la universidad; el código no cambia.

## 3. Backend

```bash
cd backend
dotnet run --launch-profile http
```

Queda en http://localhost:5015.

### Correo de recuperación de contraseña

En desarrollo no hace falta configurar nada: el enlace de «Restablece contraseña» no se envía, se
escribe en la consola del backend (busca «MODO DESARROLLO»). Para enviarlo de verdad, configura un
servidor SMTP autorizado por la universidad:

```bash
dotnet user-secrets set "Smtp:Host" "<servidor>" --project backend
dotnet user-secrets set "Smtp:Port" "587" --project backend
dotnet user-secrets set "Smtp:EnableSsl" "true" --project backend
dotnet user-secrets set "Smtp:From" "<correo remitente>" --project backend
dotnet user-secrets set "Smtp:User" "<usuario>" --project backend
dotnet user-secrets set "Smtp:Password" "<contraseña>" --project backend
```

Fuera de desarrollo también hay que indicar la dirección pública del sitio, para armar el enlace:
`dotnet user-secrets set "Seguridad:UrlPublica" "https://<dominio>"`. Sin SMTP, en producción la
recuperación responde que no está disponible.

### Cuentas de prueba

En modo desarrollo, la primera vez que arranca con la base sin usuarios, el backend siembra estos
datos (`backend/Data/SeederDesarrollo.cs`). Todas las cuentas usan la contraseña `Prueba2026!`.
Los egresados entran por http://localhost:3000/acceso-egresados; los demás, por el login general.

| Correo | Rol | Clave | Nota |
|---|---|---|---|
| `admin@uaslp.mx` | Administrador | — | |
| `crimo@uaslp.mx` | Coordinador | 100202 | Carlos Rivera Morales; también es profesor |
| `atora@uaslp.mx` | Profesor | 100201 | Ana Torres Ramírez |
| `lgofe@uaslp.mx` | Profesor | 100203 | Laura Gómez Fernández |
| `mortiz@uaq.mx` | Profesor | EXT-001 | Miguel Ortiz Luna, externo (UAQ), vigente hasta 2027-07-31 |
| `a300101@alumnos.uaslp.mx` | Alumno | 300101 | José Hernández Castillo, generación 2025-2027, 3.er semestre |
| `a300102@alumnos.uaslp.mx` | Alumno | 300102 | Mariana López Ruiz, generación 2026-2028, 1.er semestre |
| `rosa.sanchez@gmail.com` | Egresado | 2022RSC01 | Rosa Sánchez Cruz, generación 2022-2024 |

También crea las generaciones 2022-2024, 2025-2027 y 2026-2028, y el periodo 2026-2027 Otoño. Para
volver a sembrar, vacía las tablas de datos (no las de catálogos) y reinicia el backend.

## 4. Frontend

La primera vez, instala las dependencias:

```bash
cd frontend
npm install
```

Para levantarlo:

```bash
cd frontend
npm run dev
```

Queda en http://localhost:3000.

Si el backend está en otra dirección, indícala con la variable de entorno `BACKEND_URL`, por
ejemplo `BACKEND_URL=http://localhost:5015 npm run dev`.

## 5. Comprobar que todo funciona

Con los dos levantados, abre http://localhost:3000/api/salud. Debe responder:

```json
{"api":"ok","baseDeDatos":"ok"}
```

| Respuesta | Qué revisar |
|---|---|
| `"baseDeDatos":"sin conexión"` | El usuario, la contraseña o los permisos sobre `plataforma_TGA` (pasos 2.1 y 2.2) |
| Error 500 al abrir la página | El backend no está levantado o no está en el puerto 5015 |
| El backend no arranca: «Falta ConnectionStrings:TgaDb» | Falta el paso 2.2 |

## Comandos de uso diario

| Qué | Dónde | Comando |
|---|---|---|
| Levantar el backend | `backend/` | `dotnet run --launch-profile http` |
| Levantar el frontend | `frontend/` | `npm run dev` |
| Ver migraciones pendientes | `Plataforma-TGA/` | `dotnet dotnet-ef migrations list --project backend` |
| Crear una migración | `Plataforma-TGA/` | `dotnet dotnet-ef migrations add <Nombre> --project backend --output-dir Data/Migraciones` |
| Compilar el backend | `backend/` | `dotnet build` |
| Compilar el frontend para producción | `frontend/` | `npm run build` |
| Revisar el código del frontend | `frontend/` | `npm run lint` |
