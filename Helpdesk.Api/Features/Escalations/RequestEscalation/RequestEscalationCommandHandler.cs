using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Helpdesk.Api.Features.Escalations.RequestEscalation
{
    public class RequestEscalationCommandHandler : IRequestHandler<RequestEscalationCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestEscalationCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result> Handle(RequestEscalationCommand command, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isSupportAgent = await _userManager.IsInRoleAsync(user, RoleNames.SupportAgent);

            if (!isSupportAgent)
                return Error.Forbidden("Ticket.EscalationRequestForbidden", "You are not authorised to request ticket escalation.");

            Ticket? ticket = await _context.Tickets
                .Include(t => t.EscalationRequests)
                .FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (ticket.AssignedToId != command.UserId)
                return Error.Forbidden("Ticket.EscalationRequestForbidden", "You can only request ticket escalatiom of tickets assigned to you.");

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

            EscalationRequest escalationRequest = new EscalationRequest(command.Reason, command.TicketId, command.UserId, command.ProposedRecipientUserId);

            ticket.RequestEscalation(escalationRequest, user.DisplayName, proposedRecipient?.DisplayName);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
