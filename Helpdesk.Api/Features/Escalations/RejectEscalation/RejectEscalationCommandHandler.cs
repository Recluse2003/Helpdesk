using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Escalations.RejectEscalation
{
    /// <summary>
    /// Handles incoming requests to reject a pending ticket escalation request. 
    /// </summary>
    public class RejectEscalationCommandHandler : IRequestHandler<RejectEscalationCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RejectEscalationCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes an escalation rejection request.
        /// </summary>
        /// <param name="command">Contains the escalation request ID, administrator ID, and optional response reason.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> stating
        /// why it failed.
        /// </returns>
        /// <remarks>
        /// The operation verifies that: 
        /// <list type=">bullet">
        /// <item>The requesting user exists and has the Administrator role.</item>
        /// <item>The escalation request exists and is waiting for a response.</item>
        /// <item>The ticket related to the escalation request exists.</item>
        /// </list>
        /// Once validated, the ticket handles the escalation rejection and the changes are saved to the database.
        /// </remarks>
        public async Task<Result> Handle(RejectEscalationCommand command, CancellationToken cancellationToken)
        {
            // Verifies the requester exists, and that they are an administrator.
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isAdministrator)
                return Error.Forbidden("Escalation.RejectRequestForbidden", "You are not authorised to reject ticket escalation.");

            // Retrieves the escalation request and verifies that it exists, and is in a waiting state. 
            EscalationRequest? escalationRequest = await _context.EscalationRequests
                .FindAsync(command.EscalationRequestId, cancellationToken);

            if (escalationRequest == null)
                return Error.NotFound("Escalation.NotFound", "Escalation request not found.");

            if (escalationRequest.Status != EscalationRequestStatus.Waiting)
                return Error.Conflict("Escalation.AlreadyResponded", "Escalation request has already been responded to.");

            // Retrieves the ticket associated with the escalation request, and verifies that it exists. 
            Ticket? ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == escalationRequest.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            // Handles the rejection of the escalation through the assoicated ticket, before persisting the changes.
            ticket.RejectEscalation(
                escalationRequest,
                command.UserId,
                user.DisplayName,
                command.ResponseReason);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
