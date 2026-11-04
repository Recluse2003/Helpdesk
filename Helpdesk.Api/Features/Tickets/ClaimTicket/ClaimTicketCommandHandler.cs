using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Tickets.ClaimTicket
{
    /// <summary>
    /// Processes incoming requests to claim a ticket. 
    /// </summary>
    /// <remarks>
    /// Verifies the requester exists and is a support agent, abd also that the ticket they are attempting to claim also 
    /// exists, and isn't already claimed. 
    /// </remarks>
    public class ClaimTicketCommandHandler : IRequestHandler<ClaimTicketCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimTicketCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes a ticket claim request.
        /// </summary>
        /// <param name="command">
        /// Contains the ID of the support agent and the ticket to claim.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed.  
        /// </returns>
        public async Task<Result> Handle(ClaimTicketCommand command, CancellationToken cancellationToken)
        {
            // Retrieves the requester, and verifies they exist and are a support support agent. 
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isSupportAgent = await _userManager.IsInRoleAsync(user, RoleNames.SupportAgent);

            if (!isSupportAgent)
                return Error.Forbidden("Ticket.TicketClaimForbidden", "You must be a support agent to claim tickets.");

            // Retrieves the ticket being claimed, verifies it exists and isn't already claimed. 
            Ticket? ticket = await _context.Tickets.FindAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (ticket.AssignedToId != null)
                return Error.Conflict("Ticket.AlreadyAssigned", "This ticket has already been assigned to a support agent.");

            // Claims the ticket, and persists the changes. 
            ticket.Claim(command.UserId, user.DisplayName);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
