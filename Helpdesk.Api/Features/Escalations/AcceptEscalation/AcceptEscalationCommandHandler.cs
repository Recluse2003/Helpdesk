using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Escalations.AcceptEscalation
{
    /// <summary>
    /// Handles incoming requests to accept a pending ticket escalation request. 
    /// </summary>
    /// <remarks>
    /// Verifies that the requesting user is an administrator, validates the escalation request and selected support 
    /// agent, before passing the escalation acceptation to the associated ticket, and persisting the changes. 
    /// </remarks>
    public class AcceptEscalationCommandHandler : IRequestHandler<AcceptEscalationCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcceptEscalationCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes an escalation acceptance request.
        /// </summary>
        /// <param name="command">Contains the escalation request ID, administrator ID, selected support agent ID and optional
        /// response reason.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> stating
        /// why it failed.
        /// </returns>
        /// <remarks>
        /// The operation verifies that:
        /// <list type="bullet">
        /// <item>The requesting user exists and has the Administrator role.</item>
        /// <item>The escalation request exists and is waiting for a response.</item>
        /// <item>The ticket related to the escalation request exists.</item>
        /// <item>The selected support agent exists and has the SupportAgent role.</item>
        /// <item>The selected support agent is not already assigned to the ticket.</item>
        /// </list>
        /// Once validated, the ticket handles the escalation acceptance and the changes are saved to the database.
        /// </remarks>
        public async Task<Result> Handle(AcceptEscalationCommand command, CancellationToken cancellationToken)
        {
            // Verifies the requester exists, and that they are an administrator.
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isAdministrator)
                return Error.Forbidden("Escalation.AcceptRequestForbidden", "You are not authorised to accept ticket escalation.");

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

            // Prevents the new selected support support from being the same as the current assigned agent. 
            if (ticket.AssignedToId == command.NewSupportAgentId)
                return Error.Conflict("Escalation.SameAssignee", "The selected support agent must not be the current support agent.");

            // Retrieves the new assigned support agent, verifies they exist, and that they have the permissions.
            ApplicationUser? newSupportAgent = await _userManager.FindByIdAsync(command.NewSupportAgentId.ToString());

            if (newSupportAgent == null)
                return Error.NotFound("Escalation.SupportAgentNotFound", "The selected support agent was not found.");

            bool isSupportAgent = await _userManager.IsInRoleAsync(newSupportAgent, RoleNames.SupportAgent);

            if (!isSupportAgent)
                return Error.Validation("Escalation.InvalidSupportAgent", "The selected user must be a support agent.");

            // Retrieves the current assigned agent, used for retrieving their display name. 
            ApplicationUser? previousSupportAgent = await _userManager.FindByIdAsync(ticket.AssignedToId.ToString());

            // Handles the acceptance of the escalation through the assoicated ticket, before persisting the changes.
            ticket.AcceptEscalation(
                escalationRequest,
                command.UserId,
                user.DisplayName,
                command.NewSupportAgentId,
                newSupportAgent.DisplayName,
                previousSupportAgent != null ? previousSupportAgent.DisplayName : "Unknown user",
                command.ResponseReason);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
