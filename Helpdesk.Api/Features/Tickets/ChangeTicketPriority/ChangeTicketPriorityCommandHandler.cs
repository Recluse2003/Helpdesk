using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Tickets.ChangeTicketPriority
{
    /// <summary>
    /// Handles the incoming commands to change a ticket priority. 
    /// </summary>
    /// <remarks>
    /// Verifies the requesting user is either an administrator, or a support agent and is assigned to the ticket. Also 
    /// validates that the ticket exists, and that it hasn't been closed. 
    /// </remarks>
    public class ChangeTicketPriorityCommandHandler : IRequestHandler<ChangeTicketPriorityCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChangeTicketPriorityCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes the change ticket priority request. 
        /// </summary>
        /// <param name="command">Contains the ID of the requester, the ID of the ticket, and the new priority.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> stating
        /// why it failed.
        /// </returns>
        public async Task<Result> Handle(ChangeTicketPriorityCommand command, CancellationToken cancellationToken)
        {
            // Retrieves the requester, and verifies they are a support agent or administrator.
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isSupportAgent = await _userManager.IsInRoleAsync(user, RoleNames.SupportAgent);

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isSupportAgent && !isAdministrator)
                return Error.Forbidden("Ticket.PriorityChangeForbidden", "You are not authorised to change ticket priority.");

            // Retrieves the ticket, and verifies it exists, and that the user has permissions to
            // change its priority
            Ticket? ticket = await _context.Tickets.FindAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            if (!isAdministrator && ticket.AssignedToId != command.UserId)
                return Error.Forbidden("Ticket.PriorityChangeForbidden", "You can only change the priority of tickets assigned to you.");

            if (ticket.Status == TicketStatus.Closed)
                return Error.Conflict("Ticket.AlreadyClosed", "You can only change the priority of tickets that are not closed.");

            ticket.ChangePriority(command.NewPriority, command.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
