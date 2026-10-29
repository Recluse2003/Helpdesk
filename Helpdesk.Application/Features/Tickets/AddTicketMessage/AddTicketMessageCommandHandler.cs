using Helpdesk.Application.Common.Interfaces;
using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Tickets.AddTicketMessage
{
    public class AddTicketMessageCommandHandler : IRequestHandler<AddTicketMessageCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddTicketMessageCommandHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
        {
            _ticketRepository = ticketRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(AddTicketMessageCommand command, CancellationToken cancellationToken)
        {
            Ticket? ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Specified ticket not found.");

            if (command.IsPublic)
                ticket.AddPublicMessage(command.Content, command.AuthorUserId);
            else
                ticket.AddInternalNote(command.Content, command.AuthorUserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
