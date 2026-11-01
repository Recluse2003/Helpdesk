using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Tickets.ChangeTicketPriority
{
    public class ChangeTicketPriorityCommandHandler : IRequestHandler<ChangeTicketPriorityCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChangeTicketPriorityCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result> Handle(ChangeTicketPriorityCommand command, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            // Determines the user's permissions.
            bool isSupportAgent = await _userManager.IsInRoleAsync(user, "SupportAgent");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, "Administrator");

            if (!isSupportAgent && !isAdministrator)
                return Error.Forbidden("Ticket.PriorityChangeForbidden", "You are not authorised to change ticket priority.");

            Ticket? ticket = await _context.Tickets.FindAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (!isAdministrator && ticket.AssignedToId != command.UserId)
                return Error.Forbidden("Ticket.PriorityChangeForbidden", "You can only change the priority of tickets assigned to you.");

            ticket.ChangePriority(command.NewPriority, command.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
