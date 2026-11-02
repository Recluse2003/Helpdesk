using FluentValidation;

namespace Helpdesk.Api.Features.Escalations.RejectEscalation
{
    public class RejectEscalationCommandValidator : AbstractValidator<RejectEscalationCommand>
    {
        public RejectEscalationCommandValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("The user accepting the escalation must be specified.");

            RuleFor(x => x.EscalationRequestId)
                .NotEmpty()
                .WithMessage("The escalation request must be specified.");

            RuleFor(x => x.ResponseReason)
                .MaximumLength(5000)
                .WithMessage("An escalation response reason cannot exceed 5000 characters.");
        }
    }
}
