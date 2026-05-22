using MyVirtualAcademy.Data;
using MyVirtualAcademy.Models;

namespace MyVirtualAcademy.API.Tests;

public static class TestDataSeeder
{
    public const string AdminEmail = "admin.test@example.com";
    public const string ProfesorEmail = "profesor.test@example.com";
    public const string EstudianteEmail = "estudiante.test@example.com";
    public const string TestPassword = "Test1234";

    public const int RolAdministradorId = 1;
    public const int RolProfesorId = 2;
    public const int RolTutorId = 3;
    public const int RolEstudianteId = 4;

    public static void Seed(MyVirtualAcademyContext ctx)
    {
        if (!ctx.Roles.Any())
        {
            ctx.Roles.AddRange(
                new Rol { IdRol = RolAdministradorId, Nombre = "Administrador" },
                new Rol { IdRol = RolProfesorId,      Nombre = "Profesor" },
                new Rol { IdRol = RolTutorId,         Nombre = "Tutor" },
                new Rol { IdRol = RolEstudianteId,    Nombre = "Estudiante" }
            );
            ctx.SaveChanges();
        }

        // IsAdmin = true requerido para satisfacer la policy AdminOnly
        // (Program.cs: RequireClaim("IsAdmin", "true")).
        SeedUser(ctx, AdminEmail,      "Admin",      "Test", RolAdministradorId, isAdmin: true);
        SeedUser(ctx, ProfesorEmail,   "Profesor",   "Test", RolProfesorId);
        SeedUser(ctx, EstudianteEmail, "Estudiante", "Test", RolEstudianteId);
    }

    private static void SeedUser(MyVirtualAcademyContext ctx, string email, string nombre, string apellidos, int idRol, bool isAdmin = false)
    {
        if (ctx.Usuarios.Any(u => u.Email == email)) return;

        var user = new Usuario
        {
            Nombre = nombre,
            Apellidos = apellidos,
            Email = email,
            Salt = string.Empty,
            Password_Hash = Array.Empty<byte>(),
            Password = string.Empty,
            PassBCrypt = BCrypt.Net.BCrypt.HashPassword(TestPassword),
            MigratedToBCrypt = true,
            FechaRegistro = DateTime.UtcNow,
            Activo = true,
            FotoPerfil = "ProfileImage_Default.jpg",
            IsAdmin = isAdmin
        };
        ctx.Usuarios.Add(user);
        ctx.SaveChanges();

        ctx.UsuariosRoles.Add(new UsuarioRol { IdUsuario = user.IdUsuario, IdRol = idRol });
        ctx.SaveChanges();
    }
}
