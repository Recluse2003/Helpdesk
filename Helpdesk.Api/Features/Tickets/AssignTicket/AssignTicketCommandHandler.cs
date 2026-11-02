using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Tickets.AssignTicket
{
    public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result> 
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignTicketCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result> Handle(AssignTicketCommand command, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            // Determines if the user is an Administrator.
            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isAdministrator)
                return Error.Forbidden("Ticket.AssignTicketForbidden", "You are not authorised to assign a ticket to another.");

            ApplicationUser? assignee = await _userManager.FindByIdAsync(command.AssigneeId.ToString());

            if (assignee == null)
                return Error.NotFound("User.NotFound", "Assignee not found.");

            bool assigneeIsSupportAgent = await _userManager.IsInRoleAsync(assignee, RoleNames.SupportAgent);

            if (!assigneeIsSupportAgent)
                return Error.Validation("Ticket.InvalidAssignee", "The selected assignee must be a support agent.");

            Ticket? ticket = await _context.Tickets.FindAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            string? oldAssigneeDisplayName = null;

            if (ticket.AssignedToId.HasValue)
            {
                ApplicationUser? oldAssignee = await _userManager.FindByIdAsync(ticket.AssignedToId.Value.ToString());

                oldAssigneeDisplayName = oldAssignee?.DisplayName ?? "Unknown user";
            }

            ticket.AssignTo(
                command.UserId, 
                user.DisplayName, 
                command.AssigneeId, 
                assignee.DisplayName, 
                oldAssigneeDisplayName);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
