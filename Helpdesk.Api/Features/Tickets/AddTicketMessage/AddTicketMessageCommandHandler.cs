using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.AddTicketMessage
{
    /// <summary>
    /// Handles commands for adding messages or internal notes to a ticket.
    /// </summary>
    /// <remarks>
    /// Retrieves the specified ticket and delegates the creation of a public message or internal note to the 
    /// <see cref="Ticket"/> entity. Persists the changes to the database.
    /// </remarks>
    public class AddTicketMessageCommandHandler : IRequestHandler<AddTicketMessageCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddTicketMessageCommandHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Processes incoming add ticket message commands. 
        /// </summary>
        /// <param name="command">Contains the ticket ID, message content, author ID and visibility setting.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if the message is added, otherwise returns a <see cref="Result"/> containing an 
        /// <see cref="Error"/> explaining why the command failed. 
        /// </returns>
        public async Task<Result> Handle(AddTicketMessageCommand command, CancellationToken cancellationToken)
        {
            // Retrieves the ticket the message is being added to, and verifies it exists. 
            Ticket? ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Specified ticket not found.");

            if (command.IsPublic)
            {
                ticket.AddPublicMessage(command.Content, command.UserId);
            }
            else
            {
                ApplicationUser? user = await _userManager.FindByIdAsync(command.UserId.ToString());

                if (user == null)
                    return Error.NotFound("User.NotFound", "User not found.");

                bool isSupportAgent = await _userManager.IsInRoleAsync(user, RoleNames.SupportAgent);

                bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

                if (!isSupportAgent && !isAdministrator)
                    return Error.Forbidden("Ticket.InternalNoteForbidden", "Only support agents and administrators can add internal notes.");

                ticket.AddInternalNote(
                    command.Content,
                    command.UserId,
                    isAdministrator);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
