using Helpdesk.Api.Shared.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Helpdesk.Api.Infrastructure.Authentication
{
    /// <summary>
    /// Provides functionality for generating and managing authentication tokens, including JWT access tokens and refresh 
    /// tokens.
    /// </summary>
    /// <remarks>
    /// Access tokens are signed using HMAC-SHA256 and contain user identity and role claims. Refresh tokens are generated 
    /// using cryptographically secure random bytes and stored as SHA-256 hashes.
    /// </remarks>
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

        /// <summary>
        /// Generates a signed JSON Web Token containing the user's identity and assigned roles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="displayName">The display name of the user.</param>
        /// <param name="roles">The assigned roles of the user.</param>
        /// <returns>A signed JWT access token.</returns>
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

        /// <summary>
        /// Generates a cryptographically secure random refresh token.
        /// </summary>
        /// <returns>A Base64-encoded refresh token.</returns>
        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generates a SHA-256 hash of the supplied refresh token.
        /// </summary>
        /// <param name="token">The raw refresh token to hash.</param>
        /// <returns>The hexadecimal representation of the token's SHA-256 hash.</returns>
        public string HashRefreshToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}