using FluentValidation;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    public class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
    {
        public ResendEmailConfirmationCommandValidator() 
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
