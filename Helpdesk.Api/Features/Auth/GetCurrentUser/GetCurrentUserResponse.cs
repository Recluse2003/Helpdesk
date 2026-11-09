namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    /// <summary>
    /// Represents the response for a <see cref="GetCurrentUserQuery"/>.
    /// </summary>
    /// <param name="Id">Unique identifier of the current user.</param>
    /// <param name="Email">Email address of the current user.</param>
    /// <param name="DisplayName">Display name of the current user.</param>
    /// <param name="Roles">Roles of the current user.</param>
    public record GetCurrentUserResponse(
        Guid Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles);
    
}
