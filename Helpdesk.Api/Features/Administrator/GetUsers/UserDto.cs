using Helpdesk.Api.Infrastructure.Persistence.Models;

namespace Helpdesk.Api.Features.Administrator.GetUsers
{
    /// <summary>
    /// Represents the details of a <see cref="ApplicationUser"/>.
    /// </summary>
    /// <param name="Id">The unique identifier of the user.</param>
    /// <param name="DisplayName">The display name of the user.</param>
    /// <param name="Email">The email of the user.</param>
    /// <param name="Role">The role of the user.</param>
    /// <param name="EmailConfirmed">Shows if the user's email has been confirmed or not.</param>
    public record UserDto(
        Guid Id,
        string DisplayName,
        string Email,
        string Role,
        bool EmailConfirmed
    );
}