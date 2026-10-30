using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    public class GetSupportQueueQueryValidator : AbstractValidator<GetSupportQueueQuery>
    {
        public GetSupportQueueQueryValidator() 
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("The page number can not be lower than 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("The page size must be between 1 and 100.");

            RuleFor(x => x.Status)
                .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
                .WithMessage("The specified ticket status is invalid.");

            RuleFor(x => x.Priority)
                .Must(priority => !priority.HasValue || Enum.IsDefined(priority.Value))
                .WithMessage("The specified ticket priority is invalid.");
        }

    }
}
