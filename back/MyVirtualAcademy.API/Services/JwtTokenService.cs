using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyVirtualAcademy.Data;
using MyVirtualAcademy.Models;

namespace MyVirtualAcademy.API.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration config;
        private readonly MyVirtualAcademyContext context;

        public JwtTokenService(IConfiguration config, MyVirtualAcademyContext context)
        {
            this.config = config;
            this.context = context;
        }

        public string GenerateAccessToken(Usuario user, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(
                double.Parse(config["JwtSettings:AccessTokenExpiryMinutes"]!));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, $"{user.Nombre} {user.Apellidos.Split(' ')[0]}"),
                new Claim(ClaimTypes.Role, role),
                new Claim("FotoPerfil", user.FotoPerfil ?? "ProfileImage_Default.jpg"),
            };

            if (user.IsAdmin)
                claims.Add(new Claim("IsAdmin", "true"));

            var token = new JwtSecurityToken(
                issuer: config["JwtSettings:Issuer"],
                audience: config["JwtSettings:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> CreateRefreshTokenAsync(int idUsuario)
        {
            // Revoke previous tokens for this user
            var existing = await context.RefreshTokens
                .Where(t => t.IdUsuario == idUsuario && !t.Revoked)
                .ToListAsync();
            foreach (var t in existing)
                t.Revoked = true;

            var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var expiry = DateTime.UtcNow.AddDays(
                double.Parse(config["JwtSettings:RefreshTokenExpiryDays"]!));

            context.RefreshTokens.Add(new RefreshToken
            {
                Token = tokenValue,
                IdUsuario = idUsuario,
                Expiry = expiry,
                Revoked = false
            });

            await context.SaveChangesAsync();
            return tokenValue;
        }

        public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
        {
            return await context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token
                                       && !t.Revoked
                                       && t.Expiry > DateTime.UtcNow);
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var rt = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (rt != null)
            {
                rt.Revoked = true;
                await context.SaveChangesAsync();
            }
        }
    }
}
