using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Tickets.AssignTicket
{
    /// <summary>
    /// Handles incoming assign ticket commands. 
    /// </summary>
    /// <remarks>
    /// Verifies the requesting user exists and is administrator, validates the user being assigned exists and is a support 
    /// agent, and verifies the ticket being assigned exists. 
    /// </remarks>
    public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result> 
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignTicketCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes an assign ticket request. 
        /// </summary>
        /// <param name="command">Contains the requster's ID, the assignee'ID, and the ID of the ticket being assigned.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed. 
        /// </returns>
        public async Task<Result> Handle(AssignTicketCommand command, CancellationToken cancellationToken)
        {
            // Retrieves the requesting user, verifies they exist, and that they are an administrator
            ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isAdministrator)
                return Error.Forbidden("Ticket.AssignTicketForbidden", "You are not authorised to assign a ticket to another.");

            // Retrieves the user being assigned, that they exists, and that they are a support agent.
            ApplicationUser? assignee = await _userManager.FindByIdAsync(command.AssigneeId.ToString());

            if (assignee == null)
                return Error.NotFound("Ticket.AssigneeNotFound", "Assignee not found.");

            bool assigneeIsSupportAgent = await _userManager.IsInRoleAsync(assignee, RoleNames.SupportAgent);

            if (!assigneeIsSupportAgent)
                return Error.Validation("Ticket.InvalidAssignee", "The selected assignee must be a support agent.");

            // Retrieves the ticket being assigned, and that they exist.
            Ticket? ticket = await _context.Tickets.FindAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            // If applicable, retrieves the previously assigned agent, and their display name. 
            string? oldAssigneeDisplayName = null;

            if (ticket.AssignedToId.HasValue)
            {
                ApplicationUser? oldAssignee = await _userManager.FindByIdAsync(ticket.AssignedToId.Value.ToString());

                oldAssigneeDisplayName = oldAssignee?.DisplayName ?? "Unknown user";
            }

            // Assigns the ticket, before persisting the changes. 
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
