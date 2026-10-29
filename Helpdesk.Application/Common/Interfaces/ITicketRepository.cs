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
        /// Retrieves a specified <see cref="Ticket"/> from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the ticket to be retrieved.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous database operation early, if required.</param>
        /// <returns>
        /// The specified <see cref="Ticket"/> if it exists, else <see langword="null"/>.
        /// </returns>
        Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Persists changes to an existing <see cref="Ticket"/> and its <see cref="TicketActivity"/>s and it's 
        /// <see cref="TicketMessage"/>s.
        /// </summary>
        /// <param name="ticket">The <see cref="Ticket"/> to have their changes to be persisted.</param>
        void Update(Ticket ticket);

        /// <summary>
        /// Retrieves a paginated list of tickets associated with the specified user.
        /// </summary>
        /// <param name="userId">The unique identifer of the user whose tickets are being retrieved.</param>
        /// <param name="pageNumber">The page number to retrieve.<</param>
        /// <param name="pageSize">The maximum number of tickets to return per page</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous database operation early, if required.</param>
        /// <returns>
        /// A paginated list of ticket summaries for the specified user.
        /// </returns>
        Task<PagedList<TicketSummaryDto>> GetTicketsByUserId(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    }
}
