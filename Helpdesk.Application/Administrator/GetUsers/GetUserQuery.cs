using Helpdesk.Application.Administrator.GetTickets;
using Helpdesk.Application.Common.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Application.Administrator.GetUsers
{
    /// <summary>
    /// Represents a request to retrieve all users for an admin. 
    /// </summary>
    /// <param name="UserId">Unique identifier of the user attempting to retrieve users. Only an admin may do this request.</param>
    /// <param name="PageNumber">The page number to retrieve.</param>
    /// <param name="PageSize">The number of entries to include per page.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="UserDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetUserQuery(Guid UserId, int PageNumber, int PageSize) : IRequest<Result<PagedList<UserDto>>>;
}
