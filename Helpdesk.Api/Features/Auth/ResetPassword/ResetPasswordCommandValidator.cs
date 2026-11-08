using FluentValidation;

namespace Helpdesk.Api.Features.Auth.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A user must be specified.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("A code must be supplied.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("A password must be specified.")
                .MinimumLength(8)
                .WithMessage("A password must contain 8 or more characters.")
                .Matches("[A-Z]")
                .WithMessage("A password must contain at least one uppercase letter.")
                .Matches("[a-z]")
                .WithMessage("A password must contain at least one lowercase letter.")
                .Matches("[0-9]")
                .WithMessage("A password must contain at least one digit.");
        }
    }
}
