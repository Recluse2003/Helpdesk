using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.CreateTicket
{
    /// <summary>
    /// Handles incoming requests for creating tickets. 
    /// </summary>
    public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<Guid>>
    {
        private readonly HelpdeskDbContext _context;

        public CreateTicketCommandHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Processes a ticket creation request. 
        /// </summary>
        /// <param name="command">Contains the ticket title, description, the ID of the user creating the ticket, 
        /// priority of the ticket.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> containing the ID of the newly created ticket, if successful, otherwise returns a
        /// <see cref="Result"/> containing an <see cref="Error"/> stating why it failed.
        /// </returns>
        public async Task<Result<Guid>> Handle(CreateTicketCommand command, CancellationToken cancellationToken)
        {
            Ticket ticket = new (
                command.Title,
                command.Description,
                command.UserId,
                command.Priority);

            await _context.Tickets.AddAsync(ticket, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return ticket.Id;
        }
    }
}
