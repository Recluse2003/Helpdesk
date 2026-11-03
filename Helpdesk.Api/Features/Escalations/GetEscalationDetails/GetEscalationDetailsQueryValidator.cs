using FluentValidation;

namespace Helpdesk.Api.Features.Escalations.GetEscalationDetails
{
    public class GetEscalationDetailsQueryValidator : AbstractValidator<GetEscalationDetailsQuery>
    {
        public GetEscalationDetailsQueryValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("The user requesting the escalation request's details must be specified.");

            RuleFor(x => x.EscalationRequestId)
                .NotEmpty()
                .WithMessage("A escalation request must be specified.");
        }
    }
}
