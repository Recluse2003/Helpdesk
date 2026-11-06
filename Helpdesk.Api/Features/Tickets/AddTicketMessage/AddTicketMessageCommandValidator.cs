using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.AddTicketMessage
{
    /// <summary>
    /// Defines validation rules for adding a message to a ticket. 
    /// </summary>
    public class AddTicketMessageCommandValidator : AbstractValidator<AddTicketMessageCommand>
    {
        public AddTicketMessageCommandValidator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("A ticket must be specified.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A ticket message author must be specified.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("A ticket message requires text.")
                .MaximumLength(5000)
                .WithMessage("A ticket message cannot exceed 5000 characters.");

            RuleFor(x => x.IsPublic) 
                .NotEmpty()
                .WithMessage("A ticket message must state its privacy level.");
        }
    }
}
