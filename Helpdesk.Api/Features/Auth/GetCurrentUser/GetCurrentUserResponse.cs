namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    public record GetCurrentUserResponse(
        Guid Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles);
    
}
