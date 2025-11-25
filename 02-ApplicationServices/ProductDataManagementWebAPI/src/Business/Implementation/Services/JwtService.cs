using Business.Interfaces.Configuration;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Business.Implementation.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings settings;
        public JwtService(IOptions<JwtSettings> options)
        {
            settings = options.Value;
        }

        public TokenDto GenerateToken(User user, Guid? activeTenantId = null, TenantRole? activeTenantRole = null)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.SystemRole.ToString()),
            };

            if (activeTenantId.HasValue)
            {
                Claim tenantClaim = new Claim(ClaimNames.ActiveTenantId, activeTenantId.Value.ToString());
                claims.Add(tenantClaim);
            }

            if (activeTenantRole.HasValue)
            {
                Claim tenantRoleClaim = new Claim(ClaimNames.ActiveTenantRole, activeTenantRole.Value.ToString());
                claims.Add(tenantRoleClaim);
            }

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenDto(tokenString, expires);
        }
    }
}