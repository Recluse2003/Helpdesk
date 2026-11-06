using FluentValidation;

namespace Helpdesk.Api.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("A display name must be specified.")
            .MaximumLength(50)
            .WithMessage("A display name must not contain more than 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("An email address must be specified")
            .EmailAddress()
            .WithMessage("An email address must be in the valid format.")
            .MaximumLength(256)
            .WithMessage("An email address must not contain more than 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit.");
    }
}