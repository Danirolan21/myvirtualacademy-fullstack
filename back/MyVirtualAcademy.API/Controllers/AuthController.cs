using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyVirtualAcademy.API.Services;
using MyVirtualAcademy.Helper;
using MyVirtualAcademy.Models;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;
        private readonly JwtTokenService tokenService;
        private readonly IConfiguration config;
        private readonly IWebHostEnvironment env;

        public AuthController(
            RepositoryMyVirtualAcademy repo,
            JwtTokenService tokenService,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            this.repo = repo;
            this.tokenService = tokenService;
            this.config = config;
            this.env = env;
        }

        public record LoginRequest(string Email, string Password);
        public record RegisterRequest(string Nombre, string Apellidos, string Email, string Password, int IdRol);

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await repo.FindUserByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Credenciales incorrectas" });

            bool valid;
            if (user.MigratedToBCrypt)
            {
                valid = HelperCryptography.VerifyPasswordBCrypt(request.Password, user.PassBCrypt!);
            }
            else
            {
                var hash = HelperCryptography.EncryptPassword(request.Password, user.Salt);
                valid = HelperCryptography.CompararArrays(hash, user.Password_Hash);
                if (valid)
                    await repo.MigratePasswordToBCryptAsync(user.IdUsuario, request.Password);
            }

            if (!valid)
                return Unauthorized(new { message = "Credenciales incorrectas" });

            var role = await repo.GetUserRoleAsync(user.IdUsuario);
            await repo.UpdateLastAccessAsync(user.IdUsuario);

            var accessToken = tokenService.GenerateAccessToken(user, role);
            var refreshToken = await tokenService.CreateRefreshTokenAsync(user.IdUsuario);

            SetRefreshCookie(refreshToken);

            return Ok(new
            {
                accessToken,
                user = new
                {
                    id = user.IdUsuario,
                    nombre = user.Nombre,
                    apellidos = user.Apellidos,
                    role,
                    fotoPerfil = user.FotoPerfil,
                    isAdmin = user.IsAdmin
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["refresh_token"];
            if (token != null)
                await tokenService.RevokeRefreshTokenAsync(token);

            Response.Cookies.Delete("refresh_token");
            return NoContent();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var token = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { message = "No hay refresh token" });

            var rt = await tokenService.ValidateRefreshTokenAsync(token);
            if (rt == null)
                return Unauthorized(new { message = "Refresh token inválido o expirado" });

            var user = await repo.FindUserAsync(rt.IdUsuario);
            if (user == null)
                return Unauthorized();

            var role = await repo.GetUserRoleAsync(user.IdUsuario);
            var newAccessToken = tokenService.GenerateAccessToken(user, role);
            var newRefreshToken = await tokenService.CreateRefreshTokenAsync(user.IdUsuario);

            SetRefreshCookie(newRefreshToken);

            return Ok(new
            {
                accessToken = newAccessToken,
                user = new
                {
                    id = user.IdUsuario,
                    nombre = user.Nombre,
                    apellidos = user.Apellidos,
                    role,
                    fotoPerfil = user.FotoPerfil,
                    isAdmin = user.IsAdmin
                }
            });
        }

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            await repo.Register(request.Nombre, request.Apellidos,
                request.Email, request.Password, request.IdRol);
            return Ok(new { message = "Usuario registrado correctamente" });
        }

        [HttpGet("roles")]
        [Authorize]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await repo.GetRolesAsync();
            return Ok(roles);
        }

        private void SetRefreshCookie(string token)
        {
            var expiryDays = double.Parse(config["JwtSettings:RefreshTokenExpiryDays"]!);
            var isProd = env.IsProduction();
            Response.Cookies.Append("refresh_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                // SameSite=None requerido para cookies cross-domain (Vercel → runasp.net)
                SameSite = isProd ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(expiryDays)
            });
        }
    }
}
