# MyVirtualAcademy

LMS (Learning Management System) separado en dos proyectos independientes:

- `/back` — ASP.NET Core 9 Web API + Entity Framework Core + SQL Server
- `/front` — Vue 3 + Vite + TypeScript

El monolito original se conserva en `/MyVirtualAcademy/` como referencia hasta que la migración esté completa.

## Prerrequisitos

- .NET 9 SDK
- Node.js 22+ y npm 10+
- SQL Server Express (SQLEXPRESS) con la base de datos `MYVIRTUALACADEMY` creada

## Arranque local

### Backend (`/back`)

```bash
cd back/MyVirtualAcademy.API

# Primera vez: ejecutar la migración SQL
# sqlcmd -S LOCALHOST\SQLEXPRESS -d MYVIRTUALACADEMY -i ../../back/migrations/001_add_bcrypt_and_refresh_tokens.sql

# Configurar variables de entorno (ver .env.example)
# Copiar appsettings.json y ajustar ConnectionStrings y JwtSettings__SecretKey

dotnet restore
dotnet run
# API disponible en https://localhost:7100
# Swagger UI en https://localhost:7100/swagger
```

### Frontend (`/front`)

```bash
cd front

# Primera vez:
cp .env.example .env.local
# Ajustar VITE_API_URL si el backend corre en otro puerto

npm install
npm run dev
# App disponible en http://localhost:5173
```

## Variables de entorno

### Backend

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__SqlMyVirtualAcademy` | Connection string de SQL Server |
| `JwtSettings__SecretKey` | Clave secreta JWT (mín. 32 caracteres) |
| `JwtSettings__Issuer` | Issuer del JWT |
| `JwtSettings__Audience` | Audience del JWT |
| `JwtSettings__AccessTokenExpiryMinutes` | Expiración del access token (default: 15) |
| `JwtSettings__RefreshTokenExpiryDays` | Expiración del refresh token (default: 7) |
| `AllowedOrigins__0` | Origen permitido por CORS (frontend URL) |

### Frontend

| Variable | Descripción |
|----------|-------------|
| `VITE_API_URL` | URL base del backend API |

## Migración de contraseñas

La primera vez que un usuario inicia sesión en el nuevo sistema, su contraseña se migra automáticamente de SHA256 a BCrypt. No se requiere ninguna acción manual por usuario.

**Nunca commitear** `appsettings.Development.json` con credenciales reales ni `.env.local`.
