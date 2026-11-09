using FluentValidation;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    /// <summary>
    /// Defines validation rules for requests for retrieving the current user's account details.
    /// </summary>
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
