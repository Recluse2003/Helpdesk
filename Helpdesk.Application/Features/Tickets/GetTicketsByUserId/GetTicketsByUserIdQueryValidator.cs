using FluentValidation;

namespace Helpdesk.Application.Features.Tickets.GetTicketsByUserId
{
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
                .GreaterThanOrEqualTo(10)
                .WithMessage("The page size can not be lower than 10.");
        }
    }
}
