using FluentValidation;

namespace Helpdesk.Api.Features.Auth.ForgotPassword
{
    /// <summary>
    /// Defines validation rules for forgot password requests. 
    /// </summary>
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator() 
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("An email address must be specified")
                .EmailAddress()
                .WithMessage("An email address must be in the valid format.")
                .MaximumLength(256)
                .WithMessage("An email address must not contain more than 256 characters.");
        }
    }
}
