using FluentValidation;

namespace Helpdesk.Api.Features.Escalations.AcceptEscalation
{
    /// <summary>
    /// Defines validation rules for accepting a ticket escalation.
    /// </summary>
    public class AcceptEscalationCommandValidator : AbstractValidator<AcceptEscalationCommand>
    {
        public AcceptEscalationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("The user accepting the escalation must be specified.");

            RuleFor(x => x.EscalationRequestId)
                .NotEmpty()
                .WithMessage("The escalation request must be specified.");

            RuleFor(x => x.NewSupportAgentId)
                .NotEmpty()
                .WithMessage("The new support agent must be specified");

            RuleFor(x => x.ResponseReason)
                .MaximumLength(5000)
                .WithMessage("An escalation response reason cannot exceed 5000 characters.");
        }
    }
}
