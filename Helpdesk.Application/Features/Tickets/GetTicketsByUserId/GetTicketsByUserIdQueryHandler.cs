using Helpdesk.Application.Common.Interfaces;
using Helpdesk.Application.Common.Results;
using MediatR;

namespace Helpdesk.Application.Features.Tickets.GetTicketsByUserId
{
    public class GetTicketsByUserIdQueryHandler : IRequestHandler<GetTicketsByUserIdQuery, Result<PagedList<TicketSummaryDto>>>
    {
        private readonly ITicketRepository _ticketRepository;

        public GetTicketsByUserIdQueryHandler(ITicketRepository ticketRepository) 
        { 
            _ticketRepository = ticketRepository;
        }

        public async Task<Result<PagedList<TicketSummaryDto>>> Handle(GetTicketsByUserIdQuery request, CancellationToken cancellationToken)
        {
            PagedList<TicketSummaryDto> tickets = await _ticketRepository.GetTicketsByUserId(
                request.UserId, 
                request.PageNumber, 
                request.PageSize, 
                cancellationToken);

            return tickets;
        }
    }
}
