using FluentValidation;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    public class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
    {
        public GetCurrentUserQueryValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A user must be specified.");
        }
    }
}
