using MyVirtualAcademy.Models;

namespace MyVirtualAcademy.Repositories
{
    /// <summary>
    /// Métodos del repositorio relacionados con usuarios, roles y autenticación.
    /// Implementado por <see cref="RepositoryMyVirtualAcademy"/>.
    /// </summary>
    public interface IUserRepository
    {
        // ─── Auth / Register ─────────────────────────────────────────────────
        // Devuelve el IdUsuario del usuario creado. El parámetro `activo`
        // permite que el admin (POST /api/users) cree usuarios listos para
        // hacer login inmediatamente, mientras que el registro público
        // (POST /api/auth/register) los deja en false por defecto.
        Task<int> Register(string nombre, string apellidos, string email, string password, int idRol, bool activo = false);
        Task<int?> GetRolIdByNombreAsync(string nombre);
        Task<bool> RolExistsAsync(int idRol);
        Task<Usuario?> FindUserByEmailAsync(string email);
        Task<Usuario> FindUserAsync(int idUsuario);
        Task MigratePasswordToBCryptAsync(int idUsuario, string plainPassword);
        Task UpdateLastAccessAsync(int idUsuario);

        // ─── Roles ───────────────────────────────────────────────────────────
        Task<string> GetUserRoleAsync(int idUsuario);
        Task AsignarRolUsuarioAsync(int idUsuario, int idRol);
        Task<List<Rol>> GetRolesAsync();

        // ─── Gestión de usuarios (admin / perfil) ───────────────────────────
        Task<Usuario> UpdateUserAsync(int idUsuario, string nombre, string apellidos, string? fotoPerfil, string? telefono);
        Task<(bool success, string? error)> UpdatePasswordAsync(int idUsuario, string currentPassword, string newPassword);
        Task<bool> ToggleUserActiveAsync(int idUsuario);
        Task<List<object>> GetAllUsersWithRolesAsync();

        // ─── Listados de profesores / estudiantes (vistas) ──────────────────
        Task<List<VistaUsuariosConRoles>> GetProfesoresYTutoresAsync();
        Task<List<VistaUsuariosConRoles>> GetProfesoresAsync();
        Task<List<VistaUsuariosConRoles>> GetAvailableStudentsForCourseAsync(int idCurso);
    }
}
