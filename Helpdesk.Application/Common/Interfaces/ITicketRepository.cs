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
    }
}
