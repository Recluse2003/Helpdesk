using FluentValidation;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
    public class GetEscalationsQueryValidator : AbstractValidator<GetEscalationsQuery>
    {
        public GetEscalationsQueryValidator() 
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("The page number can not be lower than 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("The page size must be between 1 and 100.");

            RuleFor(x => x.Status)
                .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
                .WithMessage("The specified escalation request status is invalid.");
        }
    }
}
