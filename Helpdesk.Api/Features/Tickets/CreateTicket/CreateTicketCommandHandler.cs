using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.CreateTicket
{
    /// <summary>
    /// Handles the execution of a customer's <see cref="CreateTicketCommand"/> request.
    /// </summary>
    public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<Guid>>
    {
        private readonly HelpdeskDbContext _context;

        public CreateTicketCommandHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

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
