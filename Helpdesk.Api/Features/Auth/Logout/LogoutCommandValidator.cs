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
            RuleFor(x => x.RefreshToken);
        }
    }
}
