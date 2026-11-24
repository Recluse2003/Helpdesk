namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Represents aggregate statistics for the tickets created by a user.
    /// </summary>
    /// <param name="TotalTickets">The total number of tickets created by the user.</param>
    /// <param name="OpenTickets">The number of tickets currently awaiting support.</param>
    /// <param name="InProgressTickets">The number of tickets currently being worked on.</param>
    /// <param name="AwaitingCustomerResponseTickets">The number of tickets currently waiting for a response.</param>
    /// <param name="ResolvedTickets">The number of tickets that have been resolved.</param>
    /// <param name="ClosedTickets">The number of tickets that have been closed.</param>
    public record TicketStatisticsDto(
        int TotalTickets, 
        int OpenTickets, 
        int InProgressTickets, 
        int AwaitingCustomerResponseTickets, 
        int ResolvedTickets, 
        int ClosedTickets);
}
