using Helpdesk.Api.Shared.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Helpdesk.Api.Infrastructure.Authentication
{

    public class TokenService : ITokenService
    {
        private readonly ApiSettings _settings;
        private readonly byte[] _signingKey;

        public TokenService(IOptions<ApiSettings> options)
        {
            _settings = options.Value;
            _signingKey = Convert.FromBase64String(_settings.Secret);

            if (_signingKey.Length < 32)
                throw new InvalidOperationException("The JWT signing key must contain at least 32 bytes.");
        }

        // Access token
        public string GenerateAccessToken(Guid userId, string email, string displayName, IEnumerable<string> roles)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(30);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Name, displayName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_signingKey),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Refresh token
        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public string HashRefreshToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}