using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.AddTicketMessage
{
    public class AddTicketMessageCommandHandler : IRequestHandler<AddTicketMessageCommand, Result>
    {
        private readonly HelpdeskDbContext _context;

        public AddTicketMessageCommandHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(AddTicketMessageCommand command, CancellationToken cancellationToken)
        {
            Ticket? ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Specified ticket not found.");

            if (command.IsPublic)
                ticket.AddPublicMessage(command.Content, command.AuthorUserId);
            else
                ticket.AddInternalNote(command.Content, command.AuthorUserId);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
