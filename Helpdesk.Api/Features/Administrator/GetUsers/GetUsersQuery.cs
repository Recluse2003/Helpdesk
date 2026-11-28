using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Administrator.GetUsers
{
    /// <summary>
    /// Represents a request to retrieve all users for an admin. 
    /// </summary>
    /// <param name="UserId">Unique identifier of the user attempting to retrieve users. Only an admin may do this request.</param>
    /// <param name="PageNumber">The page number to retrieve.</param>
    /// <param name="PageSize">The number of entries to include per page.</param>
    /// <param name="SearchTerm">An optional string used to filter the results based on if the search term contained in the 
    /// user's ID, email, or display name.</param>
    /// <param name="Role">An optional string used to filter the results by if the user's have a specified role.</param>
    /// <param name="EmailConfirmed">An optional boolean used to filter the results based on if the users email address has 
    /// been confirmed or not.</param>
    /// <param name="IsActive">An optional boolean used to filter the results based on if the users are active or not.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="UserDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetUsersQuery(
        Guid UserId, 
        int PageNumber, 
        int PageSize, 
        string? SearchTerm, 
        string? Role, 
        bool? EmailConfirmed, 
        bool? IsActive
    ) : IRequest<Result<PagedList<UserDto>>>;
}
