using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    /// <summary>
    /// Handles incoming queries for retrieving the details and timeline of a ticket.
    /// </summary>
    /// <remarks>
    /// Verifies that the requesting user exists and has an authorised role. Customers can only view tickets they created 
    /// and their public messages and activities. Support agents and administrators can view ticket details, including 
    /// internal messages and activities. Combines messages and activities into a chronological timeline before returning 
    /// the ticket details.
    /// </remarks>
    public class GetTicketDetailsQueryHandler : IRequestHandler<GetTicketDetailsQuery, Result<TicketDetailsDto>>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetTicketDetailsQueryHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Retrieves ticket details and its chronological timeline for an authorised user.
        /// </summary>
        /// <param name="request">The query containing the ticket and requesting user identifiers.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous database operations.</param>
        /// <returns>
        /// A <see cref="Result"/> containg the ticket details, which contains a collectiong containg a timeline if successful,
        /// otherwise, returns a <see cref="Result"/> containing an <see cref="Error"/> explaining why the request failed. 
        /// </returns>
        public async Task<Result<TicketDetailsDto>> Handle(GetTicketDetailsQuery request, CancellationToken cancellationToken)
        {
            // Retrieves the requesting user, and verifies their permissions. 
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            // Determines the user's permissions.
            bool isCustomer = await _userManager.IsInRoleAsync(user, RoleNames.Customer);

            bool isSupportAgent = await _userManager.IsInRoleAsync(user, RoleNames.SupportAgent);

            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);

            if (!isCustomer && !isSupportAgent && !isAdministrator)
                return Error.Unauthorized("User.Unauthorised", "You are not authorised to view tickets.");

            bool canViewInternalContent = isSupportAgent || isAdministrator;

            // Retrieve the ticket, applying ownership restrictions.
            var ticketQuery = _context.Tickets
                .AsNoTracking()
                .Where(t => t.Id == request.TicketId);

            if (!canViewInternalContent)
            {
                ticketQuery = ticketQuery.Where(t => t.CreatedById == request.UserId);
            }

            var ticket = await ticketQuery
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.CreatedAt,
                    t.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            // Retrieve the ticket's messages.
            var messagesQuery = _context.TicketMessages
                .AsNoTracking()
                .Where(m => m.TicketId == request.TicketId);

            if (!canViewInternalContent)
                messagesQuery = messagesQuery.Where(m => m.IsPublic);

            var messages = await messagesQuery
                .Join(
                    _context.Users,
                    message => message.AuthorUserId,
                    author => author.Id,
                    (message, author) => new TicketTimelineItemDto(
                        message.Id,
                        TicketTimelineItemType.Message,
                        author.DisplayName,
                        message.CreatedAt,
                        message.Content,
                        null, 
                        null
                    ))
                .ToListAsync(cancellationToken);

            // Retrieve the ticket's activities.
            var activitiesQuery = _context.TicketActivities
                .AsNoTracking()
                .Where(a => a.TicketId == request.TicketId);

            if (!canViewInternalContent)
                activitiesQuery = activitiesQuery.Where(a => a.IsPublic);

            var activities = await activitiesQuery
                .ToListAsync(cancellationToken);

            // Retrieve the names of activity actors.
            var actorIds = activities
                .Where(a => a.ActorUserId.HasValue)
                .Select(a => a.ActorUserId!.Value)
                .Distinct()
                .ToList();

            var actors = await _context.Users
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.DisplayName,
                    cancellationToken);

            // Convert activities into timeline items.
            var activityItems = activities.Select(a =>
               new TicketTimelineItemDto(
                   a.Id,
                   TicketTimelineItemType.Activity,
                   a.ActorUserId.HasValue &&
                   actors.TryGetValue(a.ActorUserId.Value, out var actorName) ? actorName : "System",
                   a.CreatedAt,
                   null,
                   a.Details,
                   a.ActivityType
               )).ToList();

            // Convert messages into timeline items, and add it to the existing timeline, then sort. 
            var timeline = messages
                .Concat(activityItems)
                .OrderBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .ToList();

            // Return the ticket details.
            var ticketDetails = new TicketDetailsDto(
                ticket.Id,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.CreatedAt,
                ticket.UpdatedAt,
                timeline
            );

            return ticketDetails;
        }
    }
}
