# MyVirtualAcademy

Un LMS (Learning Management System) académico construido desde cero. Gestiona cursos, asignaturas, contenidos y entregas de tareas para tres tipos de usuario: administradores, profesores y estudiantes. Nació como monolito MVC y fue migrado a una arquitectura desacoplada con API REST + SPA.

---

## Stack

| Capa | Tecnología |
|------|-----------|
| API | ASP.NET Core 9, Entity Framework Core 9 |
| Base de datos | SQL Server (esquema propio, sin migraciones EF) |
| Auth | JWT (access token en memoria) + cookie httpOnly (refresh token), BCrypt |
| Frontend | Vue 3 + Vite + TypeScript, Pinia, Vue Router 4, Axios |

---

## Qué puedes hacer según tu rol

### Administrador
- Dashboard con métricas globales (cursos activos, inscripciones, entregas pendientes) y un feed de actividad reciente
- Gestión completa de cursos: crear, editar, archivar, borrar, subir portada
- Tabla de usuarios con toggle activo/inactivo y filtrado por rol
- Registro de nuevos usuarios asignándoles rol

### Profesor / Tutor
- Vista de tus asignaturas y cursos asociados
- Acordeón de temas por asignatura; añadir temas y contenidos (vídeo, documento, enlace, tarea)
- Calificar entregas de estudiantes con comentario de retroalimentación

### Estudiante
- Vista de asignaturas inscritas con barras de progreso por contenido completado
- Acceso a vídeos, documentos y enlaces externos
- Entrega y reentrega de tareas con countdown hasta la fecha límite

### Todos los roles
- Perfil unificado con edición inline (nombre, apellidos, teléfono, avatar) y cambio de contraseña
- Notificaciones en tiempo real: campanita en la barra con badge de no leídas, poll cada 60 s
- Calendario con eventos por tipo de contenido (tareas, exámenes, vídeos…)

---

## Principales vistas

```
/ ──────────────── Landing pública
/login ─────────── Formulario de acceso
/admin ─────────── Dashboard + gestión de cursos y usuarios (solo admin)
/admin/cursos/:id ─ Detalle de curso con asignaturas y estadísticas
/profesor ──────── Tus asignaturas y cursos
/asignatura/:id ── Acordeón de temas y contenidos, panel de creación
/estudiante ────── Mis asignaturas con progreso
/contenido/:id/* ── Tarea / Vídeo / Documento / Enlace (vista específica)
/perfil ────────── Perfil propio, modo edición inline
/calendario ────── Calendario mensual de eventos
```

---

## Instalación y arranque

### Requisitos previos

- .NET 9 SDK
- Node.js 20+ con npm
- SQL Server (SQLEXPRESS o cualquier instancia)

### 1. Base de datos

Restaura o ejecuta el DDL completo:

```bash
# Esquema inicial
sqlcmd -S LOCALHOST\SQLEXPRESS -d MyVirtualAcademy -i back/script.sql

# Migraciones incrementales (en orden)
sqlcmd -S LOCALHOST\SQLEXPRESS -d MyVirtualAcademy -i back/migrations/001_add_bcrypt_and_refresh_tokens.sql
sqlcmd -S LOCALHOST\SQLEXPRESS -d MyVirtualAcademy -i back/migrations/002_replace_notifications_table.sql
sqlcmd -S LOCALHOST\SQLEXPRESS -d MyVirtualAcademy -i back/migrations/003_ensure_isadmin_column.sql
```

La migración 001 es obligatoria antes del primer arranque: añade las columnas de BCrypt, IsAdmin y la tabla RefreshTokens. Sin ella el login falla.

### 2. Backend

```bash
cd back/MyVirtualAcademy.API
dotnet restore
dotnet run
```

API disponible en `http://localhost:5100`.  
Swagger en `http://localhost:5100/swagger`.

### 3. Frontend

```bash
cd front
cp .env.example .env.local   # solo la primera vez
npm install
npm run dev
```

App disponible en `http://localhost:5173`.  
El proxy de Vite redirige `/api/*` al backend automáticamente en desarrollo.

---

## Variables de entorno

### Backend — `appsettings.json`

| Clave | Descripción |
|-------|-------------|
| `ConnectionStrings__SqlMyVirtualAcademy` | Connection string de SQL Server |
| `JwtSettings__SecretKey` | Secreto JWT (mínimo 32 caracteres) |
| `JwtSettings__Issuer` | Issuer del token |
| `JwtSettings__Audience` | Audience del token |
| `JwtSettings__AccessTokenExpiryMinutes` | Duración del access token (defecto: 15) |
| `JwtSettings__RefreshTokenExpiryDays` | Duración del refresh token (defecto: 7) |
| `AllowedOrigins__0` | Origen del frontend para CORS en producción |

En producción usa variables de entorno del sistema en lugar de editar `appsettings.json`.

### Frontend — `.env.local`

| Variable | Descripción |
|----------|-------------|
| `VITE_API_URL` | URL base del backend (solo necesaria en producción) |

---

## Credenciales de prueba

| Rol | Email | Contraseña |
|-----|-------|------------|
| Administrador | admin@mva.com | admin123 |
| Profesor | profesor@mva.com | prof123 |
| Estudiante | estudiante@mva.com | est123 |

Las contraseñas se migran automáticamente de SHA-256 a BCrypt en el primer login. No es necesaria ninguna acción manual.

---

## Seguridad relevante

- **Rate limiting** en `POST /api/auth/login`: máximo 10 intentos por IP por minuto (429 si se supera)
- **Refresh token** en cookie httpOnly — el frontend nunca lo toca
- **BCrypt** (coste 12) para almacenamiento de contraseñas; migración lazy desde el hash legacy
- **Nombres de fichero** generados con UUID — nunca se usa el nombre original del cliente
- **IsAdmin** determinado por columna en BD, no por ID hardcodeado

---

## Estructura del repositorio

```
back/
  MyVirtualAcademy.API/
    Controllers/        Un controller por recurso (Auth, Courses, Subjects, Tasks…)
    Models/             Entidades EF Core
    Repositories/       Toda la lógica de acceso a datos en un único repositorio
    Services/           JwtTokenService, NotificationService
    Helper/             HelperPathProvider, HelperCryptography
  migrations/           Scripts SQL incrementales numerados
  script.sql            DDL completo (referencia)

front/
  src/
    api/                Módulos Axios por recurso
    components/         AppNavBar, CourseCard, ContentForm, CountdownTimer…
    router/             Vue Router 4 con guards por rol
    stores/             Pinia — auth con silent refresh
    types/              Interfaces TypeScript (Usuario, Curso, Asignatura…)
    views/              Vistas organizadas por rol (admin/, profesor/, student/, content/)
```

---

## Estado del proyecto

En desarrollo activo. Funcionalidad principal completa para los tres roles. Pendiente: módulo de exámenes, búsqueda/filtrado de contenidos y paginación de listas.
