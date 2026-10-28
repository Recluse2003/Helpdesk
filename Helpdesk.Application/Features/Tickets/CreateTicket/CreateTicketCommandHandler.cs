using Helpdesk.Application.Common.Interfaces;
using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Tickets.CreateTicket
{
    /// <summary>
    /// Handles the execution of a player's <see cref="CreateTicketCommand"/> request.
    /// Validates the user exists, 
    /// </summary>
    public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<Guid>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTicketCommandHandler(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
        {
            _ticketRepository = ticketRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateTicketCommand command, CancellationToken cancellationToken)
        {
            Ticket ticket = new (
                command.Title,
                command.Description,
                command.UserId,
                command.Priority);

            await _ticketRepository.AddAsync(ticket, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ticket.Id;
        }
    }
}
