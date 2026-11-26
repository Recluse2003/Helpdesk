using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Agents.GetAgentDashboard
{
    /// <summary>
    /// Handles incoming queries for retrieving agents' dashboards. 
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public class GetAgentDashboardQueryHandler : IRequestHandler<GetAgentDashboardQuery, Result<AgentDashboardDto>>
    {
        private readonly HelpdeskDbContext _context;

        public GetAgentDashboardQueryHandler(HelpdeskDbContext context) 
        { 
            _context = context; 
        }

        public async Task<Result<AgentDashboardDto>> Handle(GetAgentDashboardQuery query, CancellationToken cancellationToken)
        {
            var assignedTicketCount = await _context.Tickets
                .CountAsync(x =>
                    x.AssignedToId == query.AgentId &&
                    x.Status != TicketStatus.Resolved &&
                    x.Status != TicketStatus.Closed,
                    cancellationToken);

            var unassignedTicketCount = await _context.Tickets
                .CountAsync(x =>
                    x.AssignedToId == null &&
                    x.Status == TicketStatus.Open,
                    cancellationToken);

            var awaitingCustomerResponseCount = await _context.Tickets
                .CountAsync(x =>
                    x.AssignedToId == query.AgentId &&
                    x.Status == TicketStatus.AwaitingCustomerResponse,
                    cancellationToken);

            var ticketsNeedingAttention = await _context.Tickets
                .AsNoTracking()
                .Where(x =>
                    x.AssignedToId == query.AgentId &&
                    x.Status != TicketStatus.Resolved &&
                    x.Status != TicketStatus.Closed)
                .GroupJoin(
                    _context.Users,
                    ticket => ticket.CreatedById,
                    user => (Guid?)user.Id,
                    (ticket, users) => new { ticket, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(),
                    (x, user) => new
                    {
                        Ticket = x.ticket,
                        CustomerName = user != null
                            ? user.DisplayName
                            : "Unknown user"
                    }
                )
                .OrderByDescending(x => x.Ticket.Priority)
                .ThenBy(x => x.Ticket.CreatedAt)
                .Take(5)
                .Select(x => new AgentDashboardTicketDto(
                    x.Ticket.Id,
                    x.Ticket.Title,
                    x.Ticket.Priority,
                    x.Ticket.Status,
                    x.Ticket.CreatedAt,
                    x.Ticket.UpdatedAt,
                    x.Ticket.CreatedById,
                    x.CustomerName, 
                    null))
                .ToListAsync(cancellationToken);

            var recentActivities = await _context.TicketActivities
                .AsNoTracking()
                .Where(activity =>
                    _context.Tickets.Any(ticket =>
                        ticket.Id == activity.TicketId &&
                        ticket.AssignedToId == query.AgentId))
                .OrderByDescending(activity => activity.CreatedAt)
                .Take(5)
                .Select(activity => new AgentRecentActivityDto(
                    activity.TicketId,
                    _context.Tickets
                        .Where(ticket => ticket.Id == activity.TicketId)
                        .Select(ticket => ticket.Title)
                        .FirstOrDefault() ?? "Unknown ticket",
                    activity.EventType,
                    activity.Details,
                    activity.CreatedAt,
                    activity.ActorUserId,
                    _context.Users
                        .Where(user => user.Id == activity.ActorUserId)
                        .Select(user => user.DisplayName)
                        .FirstOrDefault(),
                    activity.TargetUserId,
                    _context.Users
                        .Where(user => user.Id == activity.TargetUserId)
                        .Select(user => user.DisplayName)
                        .FirstOrDefault()
                ))
                .ToListAsync(cancellationToken);

            return new AgentDashboardDto(
                assignedTicketCount,
                unassignedTicketCount, 
                awaitingCustomerResponseCount, 
                ticketsNeedingAttention, 
                recentActivities);
        }
    }
}
