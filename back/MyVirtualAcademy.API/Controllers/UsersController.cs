using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Helper;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository repo;
        private readonly HelperPathProvider helperPath;

        public UsersController(IUserRepository repo, HelperPathProvider helperPath)
        {
            this.repo = repo;
            this.helperPath = helperPath;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await repo.GetAllUsersWithRolesAsync();
            return Ok(users); // already grouped by user with concatenated roles
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (request is null)
                return BadRequest(new { message = "Datos no proporcionados" });

            var nombre = request.Nombre?.Trim() ?? "";
            var apellidos = request.Apellidos?.Trim() ?? "";
            var email = request.Email?.Trim() ?? "";
            var password = request.Password ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest(new { message = "El nombre es obligatorio" });
            if (string.IsNullOrWhiteSpace(apellidos))
                return BadRequest(new { message = "Los apellidos son obligatorios" });
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                return BadRequest(new { message = "El email no es válido" });
            if (password.Length < 6)
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres" });
            if (!await repo.RolExistsAsync(request.IdRol))
                return BadRequest(new { message = "El rol seleccionado no existe" });

            var existing = await repo.FindUserByEmailAsync(email);
            if (existing is not null)
                return Conflict(new { message = "Ya existe una cuenta con este email" });

            // activo: true → el usuario puede hacer login inmediatamente sin
            // que el admin tenga que activarlo a mano en un segundo paso.
            var idUsuario = await repo.Register(nombre, apellidos, email, password, request.IdRol, activo: true);
            return StatusCode(StatusCodes.Status201Created,
                new { idUsuario, message = "Usuario creado correctamente" });
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public record CreateUserRequest(
            string Nombre,
            string Apellidos,
            string Email,
            string Password,
            int IdRol);

        [HttpGet("profesores")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetProfesores()
        {
            var profesores = await repo.GetProfesoresYTutoresAsync();
            return Ok(profesores.Select(p => new
            {
                idUsuario = p.IdUsuario,
                nombre = p.Nombre,
                apellidos = p.Apellidos,
                nombreCompleto = $"{p.Nombre} {p.Apellidos}"
            }));
        }

        [HttpPut("{id:int}/toggle-active")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var ok = await repo.ToggleUserActiveAsync(id);
            if (!ok) return NotFound();
            return Ok(new { message = "Estado del usuario actualizado" });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.HasClaim("IsAdmin", "true");

            if (requesterId != id && !isAdmin)
                return Forbid();

            var user = await repo.FindUserAsync(id);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.IdUsuario,
                user.Nombre,
                user.Apellidos,
                user.Email,
                user.Telefono,
                user.FechaRegistro,
                user.UltimoAcceso,
                fotoPerfilUrl = helperPath.MapUrlPath(user.FotoPerfil, Folders.users)
            });
        }

        [HttpPut("{id:int}")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserRequest request)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.HasClaim("IsAdmin", "true");

            if (requesterId != id && !isAdmin)
                return Forbid();

            // Password change (only if at least one password field is provided)
            var hasCurrent = !string.IsNullOrEmpty(request.ContrasenaActual);
            var hasNew     = !string.IsNullOrEmpty(request.NuevaContrasena);
            if (hasCurrent || hasNew)
            {
                if (!hasCurrent || !hasNew)
                    return BadRequest(new { message = "Debes rellenar la contraseña actual y la nueva." });

                var (ok, error) = await repo.UpdatePasswordAsync(id, request.ContrasenaActual!, request.NuevaContrasena!);
                if (!ok) return BadRequest(new { message = error });
            }

            string? fileName = null;
            if (request.FotoPerfil != null)
            {
                var ext = Path.GetExtension(request.FotoPerfil.FileName).ToLower();
                string[] allowed = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
                if (!allowed.Contains(ext))
                    return BadRequest(new { message = "Formato de imagen no permitido." });

                fileName = Guid.NewGuid().ToString("N") + ext;
                var path = helperPath.MapPath(fileName, Folders.users);
                using var stream = new FileStream(path, FileMode.Create);
                await request.FotoPerfil.CopyToAsync(stream);
            }

            var updated = await repo.UpdateUserAsync(id,
                request.Nombre, request.Apellidos, fileName,
                string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono);

            return Ok(new { updated.IdUsuario, updated.Nombre, updated.Apellidos, updated.FotoPerfil });
        }

        public record UpdateUserRequest(
            string Nombre,
            string Apellidos,
            string? Telefono,
            IFormFile? FotoPerfil,
            string? ContrasenaActual,
            string? NuevaContrasena);
    }
}
