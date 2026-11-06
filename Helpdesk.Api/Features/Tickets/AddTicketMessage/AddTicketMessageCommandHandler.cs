using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;
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

        public AddTicketMessageCommandHandler(HelpdeskDbContext context)
        {
            _context = context;
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

            // If the message is private, delegates it to the add internal note method, else makes a public message.
            if (command.IsPublic)
                ticket.AddPublicMessage(command.Content, command.UserId);
            else
                ticket.AddInternalNote(command.Content, command.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
