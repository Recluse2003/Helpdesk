using FluentValidation;

namespace Helpdesk.Application.Features.Tickets.CreateTicket
{
    public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
    {
        public CreateTicketCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A ticket creator must be specified.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Ticket title is required.")
                .MaximumLength(200)
                .WithMessage("Ticket title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Ticket description is required.")
                .MaximumLength(5000)
                .WithMessage("Ticket description cannot exceed 5000 characters.");

            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("The specified priority is invalid.");
        }
    }
}
