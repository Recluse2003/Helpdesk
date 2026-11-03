using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Helpdesk.Api.Features.Escalations.AcceptEscalation
{
    public class AcceptEscalationCommandHandler : IRequestHandler<AcceptEscalationCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcceptEscalationCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result> Handle(AcceptEscalationCommand command, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isAdministrator)
                return Error.Forbidden("Escalation.AcceptRequestForbidden", "You are not authorised to accept ticket escalation.");

            EscalationRequest? escalationRequest = await _context.EscalationRequests
                .FindAsync(command.EscalationRequestId, cancellationToken);

            if (escalationRequest == null)
                return Error.NotFound("Escalation.NotFound", "Escalation request not found.");

            if (escalationRequest.Status != EscalationRequestStatus.Waiting)
                return Error.Conflict("Escalation.AlreadyResponded", "Escalation request has already been responded to.");

            Ticket? ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == escalationRequest.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (ticket.AssignedToId == command.NewSupportAgentId)
                return Error.Conflict("Escalation.SameAssignee", "The selected support agent must not be the current support agent.");

            ApplicationUser? newSupportAgent = await _userManager.FindByIdAsync(command.NewSupportAgentId.ToString());

            if (newSupportAgent == null)
                return Error.NotFound("Escalation.SupportAgentNotFound", "The selected support agent was not found.");

            bool isSupportAgent = await _userManager.IsInRoleAsync(newSupportAgent, RoleNames.SupportAgent);

            if (!isSupportAgent)
                return Error.Validation("Escalation.InvalidSupportAgent", "The selected user must be a support agent.");

            ApplicationUser? previousSupportAgent = await _userManager.FindByIdAsync(ticket.AssignedToId.ToString());

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
