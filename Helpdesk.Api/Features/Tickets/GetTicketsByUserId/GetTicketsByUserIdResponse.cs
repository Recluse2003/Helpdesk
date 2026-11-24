using Helpdesk.Api.Domain.Common;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Represents the response returned when retrieving a user's tickets, including the paginated ticket
    /// summaries and aggregate ticket statistics.
    /// </summary>
    /// <param name="Tickets">The paginated collection of ticket summaries belonging to the user.</param>
    /// <param name="Statistics">Aggregate statistics covering all tickets belonging to the user, regardless of the current
    /// pagination page.</param>
    public record GetTicketsByUserIdResponse(
        PagedList<TicketSummaryDto> Tickets,
        TicketStatisticsDto Statistics
    );
}
