using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.ClaimTicket
{
    /// <summary>
    /// Defines validation rules for claiming a ticket.
    /// </summary>
    public class ClaimTicketCommandHandlerValidator : AbstractValidator<ClaimTicketCommand> 
    {
        public ClaimTicketCommandHandlerValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A support agent must be specified.");

            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("A ticket must be specified.");
        }
    }
}
