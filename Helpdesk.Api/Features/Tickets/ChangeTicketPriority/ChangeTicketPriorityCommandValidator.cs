using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.ChangeTicketPriority
{
    public class ChangeTicketPriorityCommandValidator : AbstractValidator<ChangeTicketPriorityCommand>
    {
        public ChangeTicketPriorityCommandValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A support agent must be specified.");

            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("A ticket must be specified");

            RuleFor(x => x.NewPriority)
                .IsInEnum()
                .WithMessage("The provided new priority is invalid.");
        }
    }
}
