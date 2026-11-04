using FluentValidation;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    public class GetTicketDetailsQueryValidator : AbstractValidator<GetTicketDetailsQuery>
    {
        public GetTicketDetailsQueryValidator() 
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("A user must be specified.");

            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("A ticket must be specified.");
        }
    }
}
