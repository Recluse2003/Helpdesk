using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    /// <summary>
    /// Handles incoming queries for retrieving the details of the current user. 
    /// </summary>
    /// <remarks>
    /// Verifies the user exists, then formulates a response containing the required details to return, including the user's ID,
    /// email address, display name, and roles. 
    /// </remarks>
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<GetCurrentUserResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Processes a query to retrieve the details of the current user. 
        /// </summary>
        /// <param name="request">Contains the user's unique identifier.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> containing current user's account details, which includes the user's ID, email address, 
        /// display name, and roles if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed. 
        /// </returns>
        public async Task<Result<GetCurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            var result = new GetCurrentUserResponse(
                user.Id,
                user.Email!,
                user.DisplayName,
                [.. roles]
            );

            return result;
        }
    }
}
