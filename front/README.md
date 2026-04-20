# MyVirtualAcademy — Frontend

SPA en Vue 3 + Vite + TypeScript. Consume la API REST de `/back` mediante Axios. En desarrollo, el proxy de Vite hace que todas las llamadas a `/api/*` lleguen al backend sin necesidad de configurar CORS.

## Requisitos

- Node.js 20 o superior
- Backend corriendo en `http://localhost:5100` (o ajustar el proxy en `vite.config.ts`)

## Desarrollo

```bash
cp .env.example .env.local
npm install
npm run dev
```

La app arranca en `http://localhost:5173`.

## Producción

```bash
npm run build
```

Los ficheros estáticos quedan en `dist/`. Servir con nginx u otro servidor estático apuntando a ese directorio. Configurar nginx para redirigir todas las rutas a `index.html` (necesario para Vue Router en modo history).

En producción el proxy de Vite no existe, así que nginx debe reenviar `/api/*` al backend o ajustar `VITE_API_URL` si el frontend llama directamente a la URL del API.

## Variables de entorno

Copiar `.env.example` a `.env.local` y ajustar:

| Variable | Descripción |
|----------|-------------|
| `VITE_API_URL` | URL del backend. En desarrollo no hace falta (el proxy lo gestiona). En producción apuntar al API real. |

## Estructura relevante

```
src/
  api/          Un fichero por recurso (auth, courses, subjects, topics, content, tasks, users)
  components/   AppNavBar, CourseCard, CourseEditModal, ContentForm, FileUploader, CountdownTimer
  router/       Vue Router 4 — guards de autenticación y roles en beforeEach
  stores/       auth.ts — Pinia store; token en sessionStorage, silentRefresh con cookie httpOnly
  types/        Interfaces TypeScript que reflejan los DTOs del backend
  views/        Organizadas por rol: admin/, profesor/, student/, content/, auth/, user/
```

## Roles y rutas

| Ruta | Acceso |
|------|--------|
| `/` | Pública |
| `/login` | Pública |
| `/register` | Admin |
| `/admin`, `/admin/cursos/*` | Admin |
| `/profesor`, `/asignatura/:id` | Profesor, Tutor, Admin |
| `/estudiante` | Autenticado |
| `/perfil`, `/perfil/editar` | Autenticado |
| `/contenido/:id/*` | Autenticado |
