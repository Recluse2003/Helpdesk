using FluentValidation;

namespace Helpdesk.Api.Features.Auth.Logout
{
    /// <summary>
    /// Defines validation rules for logout requests.
    /// </summary>
    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator() 
        { 
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("A refresh token must be provided.");
        }
    }
}
