using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Defines validation rules for retrieving tickets associated with a specified user. 
    /// </summary>
    public class GetTicketsByUserIdQueryValidator : AbstractValidator<GetTicketsByUserIdQuery>
    {
        public GetTicketsByUserIdQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A ticket creator must be specified.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("The page number can not be lower than 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("The page size must be between 1 and 100.");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200)
                .WithMessage("The search term may not be longer than 200 characters.");

            RuleFor(x => x.Status)
                .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
                .WithMessage("The specified ticket status is invalid.");
        }
    }
}
