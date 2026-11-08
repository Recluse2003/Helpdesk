using FluentValidation;

namespace Helpdesk.Api.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A user must be specified.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("A code must be supplied.");
        }
    }
}
