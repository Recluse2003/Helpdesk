using FluentValidation;

namespace Helpdesk.Api.Features.Auth.Refresh
{
    /// <summary>
    /// Defines validation rules for refresh token requests.
    /// </summary>
    public class RefreshCommandValidator : AbstractValidator<RefreshCommand>
    {
        public RefreshCommandValidator() 
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("A refresh token must be provided.");
        }
    }
}
