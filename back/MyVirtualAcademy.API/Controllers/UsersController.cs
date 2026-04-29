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
        private readonly RepositoryMyVirtualAcademy repo;
        private readonly HelperPathProvider helperPath;

        public UsersController(RepositoryMyVirtualAcademy repo, HelperPathProvider helperPath)
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

                fileName = request.FotoPerfil.FileName;
                var path = helperPath.MapPath(fileName, Folders.users);
                using var stream = new FileStream(path, FileMode.Create);
                await request.FotoPerfil.CopyToAsync(stream);
            }

            var updated = await repo.UpdateUserAsync(id,
                request.Nombre, request.Apellidos, fileName, request.Telefono ?? string.Empty);

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
