# Plataforma TGA

Seguimiento de alumnos, titulación y egresados de la Maestría en Tecnología y Gestión del Agua
(Facultad de Ingeniería, UASLP).

| Carpeta | Qué es |
|---|---|
| `backend/` | API en ASP.NET Core (.NET 10, EF Core 10) |
| `frontend/` | Interfaz en Next.js 16 (TypeScript y Tailwind) |
| `database/` | `esquema.sql`: crea la base `plataforma_TGA` con sus tablas y catálogos |

El navegador solo habla con Next.js; las peticiones a `/api/*` las reenvía Next.js al backend.

## Requisitos

| Herramienta | Versión | Comprobar |
|---|---|---|
| .NET SDK | 10 | `dotnet --list-sdks` |
| Node.js | 22 o superior | `node -v` |
| MySQL | 9 | `mysql --version` |

## 1. Base de datos

Se hace una sola vez. Desde esta carpeta, como root de MySQL:

```bash
mysql -u root -p < database/esquema.sql
```

Crea el usuario que usará la aplicación y dale permisos sobre la base. Si ya existe, omite el
`CREATE USER`:

```sql
CREATE USER 'admin'@'localhost' IDENTIFIED BY '<contraseña>';
GRANT SELECT, INSERT, UPDATE, DELETE ON plataforma_TGA.* TO 'admin'@'localhost';
```

> En Linux, MySQL distingue mayúsculas en el nombre de la base: siempre es `plataforma_TGA`.

También puedes abrir `database/esquema.sql` en MySQL Workbench, conectado como root, y ejecutarlo
con el botón del rayo (Execute).

## 2. Backend

La cadena de conexión no va en el repositorio: se guarda con `dotnet user-secrets`. Se hace una sola
vez por máquina:

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:TgaDb" "Server=localhost;Port=3306;Database=plataforma_TGA;User=admin;Password=<contraseña>;"
```

Para levantarlo:

```bash
cd backend
dotnet run --launch-profile http
```

Queda en http://localhost:5015.

## 3. Frontend

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

## 4. Comprobar que todo funciona

Con los dos levantados, abre http://localhost:3000/api/salud. Debe responder:

```json
{"api":"ok","baseDeDatos":"ok"}
```

| Respuesta | Qué revisar |
|---|---|
| `"baseDeDatos":"sin conexión"` | El usuario, la contraseña o los permisos sobre `plataforma_TGA` (pasos 1 y 2) |
| Error 500 al abrir la página | El backend no está levantado o no está en el puerto 5015 |
| El backend no arranca: «Falta ConnectionStrings:TgaDb» | Falta el `user-secrets set` del paso 2 |

## Comandos de uso diario

| Qué | Dónde | Comando |
|---|---|---|
| Levantar el backend | `backend/` | `dotnet run --launch-profile http` |
| Levantar el frontend | `frontend/` | `npm run dev` |
| Compilar el backend | `backend/` | `dotnet build` |
| Compilar el frontend para producción | `frontend/` | `npm run build` |
| Revisar el código del frontend | `frontend/` | `npm run lint` |
