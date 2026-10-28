using Helpdesk.Application.Common.Results;
using Helpdesk.Application.Features.Tickets.GetTicketsByUserId;
using Helpdesk.Domain.Entities;

namespace Helpdesk.Application.Common.Interfaces
{
    /// <summary>
    /// Provides persistence operations for <see cref="Ticket"/>.
    /// </summary>
    public interface ITicketRepository
    {
        /// <summary>
        /// Persists a new <see cref="Ticket"/> to the database. 
        /// </summary>
        /// <param name="ticket">The <see cref="Ticket"/> to be persisted.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous database operation early, if required.</param>
        Task AddAsync(Ticket ticket, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a page
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PagedList<TicketSummaryDto>> GetTicketsByUserId(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    }
}
