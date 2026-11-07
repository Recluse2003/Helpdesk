namespace Helpdesk.Api.Shared.Interfaces
{
    public interface ITokenService
    {
        GeneratedToken GenerateToken(Guid userId, string email, string displayName, IEnumerable<string> roles);
    }

    public record GeneratedToken(string AccessToken, DateTime ExpiresAt);
}
