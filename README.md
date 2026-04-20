# MyVirtualAcademy

LMS académico separado en dos proyectos independientes:

- `/back` — API REST en ASP.NET Core 9, Entity Framework Core 9, SQL Server
- `/front` — SPA en Vue 3 + Vite + TypeScript

El monolito original está en `/MyVirtualAcademy/` como referencia de lectura, excluido del repositorio.

## Prerrequisitos

- .NET 9 SDK
- Node.js 20 o superior con npm
- SQL Server (SQLEXPRESS o cualquier instancia) con la base de datos `MYVIRTUALACADEMY` restaurada

La base de datos parte del esquema en `back/script.sql`. Antes del primer arranque del API hay que ejecutar también el script de migración:

```bash
sqlcmd -S LOCALHOST\SQLEXPRESS -d MYVIRTUALACADEMY \
  -i back/migrations/001_add_bcrypt_and_refresh_tokens.sql
```

Ese script añade las columnas `Pass_BCrypt`, `MigratedToBCrypt` e `IsAdmin` a la tabla `Usuarios`, marca al usuario con `ID_Usuario = 1` como administrador, y crea la tabla `RefreshTokens`. Sin ejecutarlo el login devuelve error de columna inexistente.

## Arranque

### Backend

```bash
cd back/MyVirtualAcademy.API
dotnet restore
dotnet run
```

El API queda en `http://localhost:5100`. Swagger disponible en `http://localhost:5100/swagger`.

Antes del primer `dotnet run` hay que editar `appsettings.json` con la connection string real y una clave JWT de al menos 32 caracteres. No se requiere `appsettings.Development.json` salvo para sobreescribir el nivel de log.

### Frontend

```bash
cd front
cp .env.example .env.local   # solo la primera vez
npm install
npm run dev
```

La app queda en `http://localhost:5173`. El proxy de Vite redirige `/api/*` al backend en desarrollo, así que no hay configuración adicional de CORS ni de baseURL mientras ambos corran localmente.

## Variables de entorno

### Backend (`appsettings.json` o variables del sistema)

| Clave | Descripción |
|-------|-------------|
| `ConnectionStrings__SqlMyVirtualAcademy` | Connection string de SQL Server |
| `JwtSettings__SecretKey` | Secreto JWT, mínimo 32 caracteres |
| `JwtSettings__Issuer` | Issuer del token |
| `JwtSettings__Audience` | Audience del token |
| `JwtSettings__AccessTokenExpiryMinutes` | Vida del access token (por defecto 15) |
| `JwtSettings__RefreshTokenExpiryDays` | Vida del refresh token (por defecto 7) |
| `AllowedOrigins__0` | Origen del frontend permitido por CORS (producción) |

No commitear credenciales reales en `appsettings.json`. En producción usar variables de entorno del sistema o un gestor de secretos.

### Frontend (`.env.local`, ignorado por git)

| Variable | Descripción |
|----------|-------------|
| `VITE_API_URL` | URL base del backend; solo necesario en producción |

## Migración de contraseñas

Las contraseñas se migran de SHA-256 a BCrypt de forma transparente en el primer login de cada usuario: el sistema verifica con el hash antiguo, re-hashea con BCrypt (coste 12) y vacía la columna `Pass` en texto plano. No se requiere ninguna acción manual por usuario.

## Estructura del repo

```
back/
  MyVirtualAcademy.API/     API REST (.NET 9)
  migrations/               Scripts SQL incrementales numerados
  script.sql                DDL completo del esquema de la BD (solo referencia)

front/
  src/
    api/                    Módulos Axios por recurso
    components/             Componentes Vue reutilizables
    router/                 Vue Router 4 con guards de autenticación
    stores/                 Pinia (store de auth con silent refresh)
    types/                  Interfaces TypeScript
    views/                  Vistas organizadas por rol y tipo de contenido
```
