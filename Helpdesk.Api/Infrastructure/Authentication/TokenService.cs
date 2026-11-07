using Helpdesk.Api.Shared.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;


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
            {
                throw new InvalidOperationException(
                    "The JWT signing key must contain at least 32 bytes.");
            }
        }

        public GeneratedToken GenerateToken(
            Guid userId,
            string email,
            string displayName,
            IEnumerable<string> roles)
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

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new GeneratedToken(accessToken, expiresAt);
        }
    }
}
