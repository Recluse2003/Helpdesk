using FluentValidation;

namespace Helpdesk.Api.Features.Escalations.RequestEscalation
{
    /// <summary>
    /// Defines validation rules for creating a ticket escalation request.
    /// </summary>
    public class RequestEscalationCommandValidator : AbstractValidator<RequestEscalationCommand>
    {
        public RequestEscalationCommandValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("The user accepting the escalation must be specified.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("An escalation request requires a reason.")
                .MaximumLength(5000)
                .WithMessage("An escalation response reason cannot exceed 5000 characters.");

            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("The ticket to be escalated must be specified.");

            RuleFor(x => x.ProposedRecipientUserId);
        }
    }
}
