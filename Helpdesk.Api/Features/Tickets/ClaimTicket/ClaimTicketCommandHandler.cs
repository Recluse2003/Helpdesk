using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Tickets.ClaimTicket
{
    public class ClaimTicketCommandHandler : IRequestHandler<ClaimTicketCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimTicketCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result> Handle(ClaimTicketCommand command, CancellationToken cancellationToken)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            // Determines if the user's permissions.
            bool isSupportAgent = await _userManager.IsInRoleAsync(user, "SupportAgent");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, "Administrator");

            if (!isSupportAgent && !isAdministrator)
                return Error.Forbidden("Ticket.TicketClaimForbidden", "You are not authorised to claim tickets.");

            Ticket? ticket = await _context.Tickets.FindAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (ticket.AssignedToId != null)
                return Error.Conflict("Ticket.AlreadyAssigned", "This ticket has already been assigned to a support agent.");

            ticket.Claim(command.UserId, user.DisplayName);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
