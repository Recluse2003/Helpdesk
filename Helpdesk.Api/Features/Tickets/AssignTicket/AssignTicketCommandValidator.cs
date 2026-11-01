using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.AssignTicket
{
    public class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
    {
        public AssignTicketCommandValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("The user performing the assignment must be specified.");

            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("A ticket must be specified");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A support agent must be specified.");
        }
    }
}
