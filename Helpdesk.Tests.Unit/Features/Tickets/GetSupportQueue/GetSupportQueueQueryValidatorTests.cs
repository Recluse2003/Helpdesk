using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Features.Tickets.GetSupportQueue;

namespace Helpdesk.Tests.Unit.Features.Tickets.GetSupportQueue
{
    public class GetSupportQueueQueryValidatorTests
    {
        private readonly GetSupportQueueQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ShouldNotHaveErrors()
        {
            var query = new GetSupportQueueQuery(
                1, 
                10, 
                TicketStatus.Open,
                TicketPriority.Medium,
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithNegativePageNumber_ShouldHaveErrors()
        {
            var query = new GetSupportQueueQuery(
                -1,
                10,
                TicketStatus.Open,
                TicketPriority.Medium,
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageNumber);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_WithInvalidPageSize_ShouldHaveErrors(int pageSize)
        {
            var query = new GetSupportQueueQuery(
                1,
                pageSize,
                TicketStatus.Open,
                TicketPriority.Medium,
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }

        [Fact]
        public void Validate_WithInvalidStatus_ShouldHaveErrors()
        {
            var query = new GetSupportQueueQuery(
                1,
                10,
                (TicketStatus)999,
                TicketPriority.Medium,
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_WithInvalidPriority_ShouldHaveErrors()
        {
            var query = new GetSupportQueueQuery(
                1,
                10,
                TicketStatus.Open,
                (TicketPriority)999,
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.Priority);
        }
    }
}
