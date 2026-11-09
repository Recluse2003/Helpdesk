namespace Helpdesk.Api.Shared.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(
            Guid userId,
            string email,
            string displayName,
            IEnumerable<string> roles);

        string GenerateRefreshToken();

        string HashRefreshToken(string token);
    }
}
