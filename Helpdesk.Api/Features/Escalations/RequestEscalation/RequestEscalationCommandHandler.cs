using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Escalations.RequestEscalation
{
    /// <summary>
    /// Handles incoming commands to create a new <see cref="EscalationRequest"/>.
    /// </summary>
    public class RequestEscalationCommandHandler : IRequestHandler<RequestEscalationCommand, Result<Guid>>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestEscalationCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes an escalation creation request
        /// </summary>
        /// <param name="command">Contains the ID of the requesting user, the reason for the escalation, ID of the ticket
        /// being escalated, and optionally the ID of a possible replacement support agent.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// A <see cref="Result"/> containing the newly created <see cref="EscalationRequest"/> unique identifier, 
        /// otherwise returns a <see cref="Result"/> containing an <see cref="Error"/> explaining why the command failed. 
        /// </returns>
        public async Task<Result<Guid>> Handle(RequestEscalationCommand command, CancellationToken cancellationToken)
        {
            // Retrieves the requesting user, and verifies they exist and are a support agent.
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isSupportAgent = await _userManager.IsInRoleAsync(user, RoleNames.SupportAgent);

            if (!isSupportAgent)
                return Error.Forbidden("Ticket.EscalationRequestForbidden", "You are not authorised to request ticket escalation.");

            // Retrieves the ticket being escalated, and verifies that it exists amd that the requester is the current assigned
            // agent to the ticket
            Ticket? ticket = await _context.Tickets
                .Include(t => t.EscalationRequests)
                .FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (ticket.AssignedToId != command.UserId)
                return Error.Forbidden("Ticket.EscalationRequestForbidden", "You can only request ticket escalatiom of tickets assigned to you.");

            // If provided, verifies the proposed new support agent isn't the requester, that they exist, and that they are 
            // a support agent. 
            ApplicationUser? proposedRecipient = null;

            if (command.ProposedRecipientUserId.HasValue)
            {
                if (command.ProposedRecipientUserId.Value == command.UserId)
                {
                    return Error.Validation("Escalation.SelfAssignmentNotAllowed", "You cannot propose yourself as the recipient of an escalation.");
                }

                proposedRecipient = await _userManager.FindByIdAsync(command.ProposedRecipientUserId.Value.ToString());

                if (proposedRecipient == null)
                    return Error.NotFound("Escalation.ProposedRecipientNotFound", "The proposed recipient was not found.");

                bool isRecipientSupportAgent = await _userManager.IsInRoleAsync(proposedRecipient, RoleNames.SupportAgent);

                if (!isRecipientSupportAgent)
                    return Error.Validation("Escalation.InvalidProposedRecipient", "The proposed recipient must be a support agent.");
            }

            // Creates the escalation request, and persists it. 
            EscalationRequest escalationRequest = new EscalationRequest(command.Reason, command.TicketId, command.UserId, command.ProposedRecipientUserId);

            ticket.RequestEscalation(escalationRequest, user.DisplayName, proposedRecipient?.DisplayName);

            await _context.SaveChangesAsync(cancellationToken);

            return escalationRequest.Id;
        }
    }
}
