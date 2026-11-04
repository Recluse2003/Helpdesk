using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Escalations.GetEscalationDetails
{
    /// <summary>
    /// Handles incoming queries for retrieving the details of a specific <see cref="EscalationRequest"/>
    /// </summary>
    /// <remarks>
    /// Ensures that the requesting user is either an administrator or the user is the author of the escalation request.
    /// Retrieves the associated ticket and relevant user information before returning an 
    /// <see cref="EscalationRequestDetailsDto"/>.
    /// </remarks>
    public class GetEscalationDetailsQueryHandler : IRequestHandler<GetEscalationDetailsQuery, Result<EscalationRequestDetailsDto>>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetEscalationDetailsQueryHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Retrieves the details of a specified escalation request.
        /// </summary>
        /// <param name="query">Contains the ID of the requesting user and the escalation request to retrieve.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> containing the escalation request details in the form of 
        /// <see cref="EscalationRequestDetailsDto"/>, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// stating why it failed.
        /// </returns>
        /// <remarks>
        /// Access is restricted to administrators and the user who originally submitted the escalation request.
        /// 
        /// The returned details include the associated ticket, requester, proposed recipient, responding administrator, 
        /// and newly assigned support agent if applicable.
        /// </remarks>
        public async Task<Result<EscalationRequestDetailsDto>> Handle(GetEscalationDetailsQuery query, CancellationToken cancellationToken)
        {
            // Retrieve requester.
            ApplicationUser? user = await _userManager.FindByIdAsync(query.UserId.ToString());

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            // Retrieve request.
            EscalationRequest? escalationRequest = await _context.EscalationRequests
                .FindAsync(query.EscalationRequestId, cancellationToken);

            if (escalationRequest == null)
                return Error.NotFound("Escalation.NotFound", "Escalation request not found.");

            // Verifies request is allowed to view request.
            bool isAdministrator = await _userManager.IsInRoleAsync(user, RoleNames.Administrator);
            bool isRequester = escalationRequest.RequestedByUserId == user.Id;

            if (!isAdministrator && !isRequester)
                return Error.Forbidden("Escalation.ReviewForbidden", "You are not authorised to view this escalation request.");

            // Retrieve ticket.
            Ticket? ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == escalationRequest.TicketId, cancellationToken);

            if (ticket == null)
                return Error.NotFound("Ticket.NotFound", "Ticket not found.");

            // Retrieve requester.
            ApplicationUser? requester = await _userManager.FindByIdAsync(escalationRequest.RequestedByUserId.ToString());

            // Retrieve proposed recipient, if one exists.
            ApplicationUser? proposedRecipient = null;

            if (escalationRequest.ProposedRecipientUserId.HasValue)
                proposedRecipient = await _userManager.FindByIdAsync(escalationRequest.ProposedRecipientUserId.Value.ToString());

            // Retrieve administrator who responded, if applicable.
            ApplicationUser? respondedBy = null;

            if (escalationRequest.RespondedByUserId.HasValue)
                respondedBy = await _userManager.FindByIdAsync(escalationRequest.RespondedByUserId.Value.ToString());

            // Retrieve the support agent who was actually assigned, if applicable.
            ApplicationUser? newSupportAgent = null;

            if (escalationRequest.NewSupportAgentId.HasValue)
                newSupportAgent = await _userManager.FindByIdAsync(escalationRequest.NewSupportAgentId.Value.ToString());

            EscalationRequestDetailsDto dto = new(
                escalationRequest.Id,
                ticket.Id,
                ticket.Title,

                escalationRequest.RequestedByUserId,
                requester?.DisplayName ?? "Unknown user",

                escalationRequest.ProposedRecipientUserId,
                proposedRecipient?.DisplayName,

                escalationRequest.Reason,

                escalationRequest.Status,

                escalationRequest.RespondedByUserId,
                respondedBy?.DisplayName,

                escalationRequest.NewSupportAgentId,
                newSupportAgent?.DisplayName,

                escalationRequest.ResponseReason,

                escalationRequest.CreatedAt,
                escalationRequest.RespondedAt
            );

            return dto;
        }
    }
}
