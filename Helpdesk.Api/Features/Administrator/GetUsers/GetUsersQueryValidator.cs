using FluentValidation;
using Helpdesk.Api.Domain.Common;

namespace Helpdesk.Api.Features.Administrator.GetUsers
{
    public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator() 
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("The page number can not be lower than 1."); ;

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("The page size must be between 1 and 100."); ;

            RuleFor(x => x.SearchTerm)
                .MaximumLength(100)
                .WithMessage("The search term cannot contain more than 100 characters.");

            RuleFor(x => x.Role)
                .Must(role => role is null || RoleNames.IsValid(role))
                .WithMessage("The specified role is invalid.");
        }
    }
}
