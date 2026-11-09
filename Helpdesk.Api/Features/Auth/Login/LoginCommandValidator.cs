using FluentValidation;

namespace Helpdesk.Api.Features.Auth.Login
{
    /// <summary>
    /// Defines validation rules for login user requests.
    /// </summary>
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator() 
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("An email address must be specified.")
                .EmailAddress()
                .WithMessage("An email address must be in the valid format.")
                .MaximumLength(256)
                .WithMessage("An email address must not contain more than 256 characters.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("A password must be specified.");
        }
    }
}
