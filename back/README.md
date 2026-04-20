# MyVirtualAcademy — Backend

API REST en ASP.NET Core 9 con autenticación JWT + refresh token en cookie httpOnly. Base de datos SQL Server accedida mediante Entity Framework Core 9 (code-first contra esquema existente, sin migraciones EF).

## Requisitos

- .NET 9 SDK
- SQL Server con la base de datos `MYVIRTUALACADEMY` restaurada desde `script.sql`
- Script de migración ejecutado (ver más abajo)

## Configuración inicial

1. Ejecutar el script de migración una sola vez:

```bash
sqlcmd -S LOCALHOST\SQLEXPRESS -d MYVIRTUALACADEMY \
  -i migrations/001_add_bcrypt_and_refresh_tokens.sql
```

Añade `Pass_BCrypt`, `MigratedToBCrypt` e `IsAdmin` a `Usuarios`, y crea la tabla `RefreshTokens`.

2. Editar `MyVirtualAcademy.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqlMyVirtualAcademy": "Data Source=LOCALHOST\\SQLEXPRESS;Initial Catalog=MYVIRTUALACADEMY;User ID=sa;Password=TU_PASSWORD;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "SecretKey": "clave-de-al-menos-32-caracteres-aleatoria"
  }
}
```

No commitear credenciales reales. En producción usar variables de entorno del sistema con la misma nomenclatura (`ConnectionStrings__SqlMyVirtualAcademy`, `JwtSettings__SecretKey`, etc.).

## Desarrollo

```bash
cd MyVirtualAcademy.API
dotnet restore
dotnet run
```

API en `http://localhost:5100`. Swagger en `http://localhost:5100/swagger`.

Para hot reload:

```bash
dotnet watch run
```

## Endpoints principales

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/auth/login` | Login; devuelve access token en body y refresh token en cookie |
| POST | `/api/auth/refresh` | Renueva el access token usando la cookie |
| POST | `/api/auth/logout` | Revoca el refresh token |
| POST | `/api/auth/register` | Registra usuario (requiere rol Admin) |
| GET | `/api/auth/roles` | Lista de roles disponibles |
| GET/PUT | `/api/users/{id}` | Perfil de usuario |
| GET | `/api/courses` | Lista cursos (Admin) |
| POST | `/api/courses` | Crear curso (Admin) |
| PUT | `/api/courses/{id}` | Editar curso (Admin) |
| GET | `/api/subjects/{id}` | Detalle de asignatura |
| GET | `/api/subjects/by-professor/{id}` | Asignaturas de un profesor |
| GET | `/api/subjects/by-student/{id}` | Asignaturas de un estudiante |
| POST | `/api/topics` | Crear tema |
| GET/POST/PUT | `/api/content/{id}` | Contenido multimedia |
| GET | `/api/tasks/{id}` | Detalle de tarea con entregas |
| POST | `/api/tasks/{id}/submit` | Entregar tarea |
| POST | `/api/tasks/{id}/grade` | Calificar entrega |

## Estructura

```
MyVirtualAcademy.API/
  Controllers/    AuthController, CoursesController, SubjectsController,
                  TopicsController, ContentController, TasksController,
                  UsersController, ExamsController, ProgressController
  Data/           MyVirtualAcademyContext (EF Core, sin migraciones EF)
  Helper/         HelperCryptography (SHA-256 + BCrypt), HelperPathProvider
  Models/         Entidades y ViewModels
  Repositories/   RepositoryMyVirtualAcademy (único repositorio, todos los métodos)
  Services/       JwtTokenService
  Program.cs      Wiring de DI, JWT, CORS, Swagger, middleware
```

## Autenticación

- Access token JWT (15 min) devuelto en el body del login.
- Refresh token (7 días) en cookie `httpOnly; SameSite=Lax`. En producción cambiar a `Secure=true; SameSite=Strict`.
- El frontend almacena el access token en `sessionStorage` y lo adjunta en cada petición como `Authorization: Bearer`.
- Si el access token expira, el interceptor Axios llama automáticamente a `/api/auth/refresh`.
- Al recargar la página, Vue llama a `/api/auth/refresh` antes de renderizar (silent refresh).

## Políticas de autorización

| Política | Roles |
|----------|-------|
| `AdminOnly` | Usuarios con claim `IsAdmin = true` |
| `ProfesorUTutor` | Administrador, Profesor, Tutor |
