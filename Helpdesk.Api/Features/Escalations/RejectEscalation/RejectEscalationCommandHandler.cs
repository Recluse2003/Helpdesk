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
    public class RejectEscalationCommandHandler : IRequestHandler<RejectEscalationCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RejectEscalationCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result> Handle(RejectEscalationCommand command, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isAdministrator)
                return Error.Forbidden("Escalation.RejectRequestForbidden", "You are not authorised to reject ticket escalation.");

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
