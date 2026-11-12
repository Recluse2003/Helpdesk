
using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Escalations.GetEscalations
{
    public class GetEscalationsQueryHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HelpdeskDbContext _context;
        private readonly GetEscalationsQueryHandler _handler;

        public GetEscalationsQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new GetEscalationsQueryHandler(
                _context,
                _userManagerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<ApplicationUser> CreateUser(string displayName = "Test User")
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = displayName
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private async Task<Ticket> CreateTicket(Guid creatorId, string title = "Test Ticket")
        {
            var ticket = new Ticket(
                title,
                "Test ticket description.",
                creatorId);

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }

        private async Task<EscalationRequest> CreateEscalationRequest(
            Guid ticketId,
            Guid requesterId,
            Guid? proposedRecipientId = null,
            EscalationRequestStatus status = EscalationRequestStatus.Waiting)
        {
            var request = new EscalationRequest(
                "Escalation reason.",
                ticketId,
                requesterId,
                proposedRecipientId);

            if (status == EscalationRequestStatus.Accepted)
            {
                request.Accept(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Escalation accepted.");
            }
            else if (status == EscalationRequestStatus.Rejected)
            {
                request.Reject(
                    Guid.NewGuid(),
                    "Escalation rejected.");
            }

            _context.EscalationRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        [Fact]
        public async Task Handle_WhenNoEscalationsExist_ReturnsEmptyPagedList()
        {
            // Arrange
            var query = new GetEscalationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
            result.Value.TotalPages.Should().Be(0);
            result.Value.HasNextPage.Should().BeFalse();
            result.Value.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WhenEscalationsExist_ReturnsCorrectDetails()
        {
            // Arrange
            var requester = await CreateUser("Requesting Agent");
            var recipient = await CreateUser("Proposed Agent");

            var ticket = await CreateTicket(requester.Id, "Password Reset Issue");

            var request = await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                recipient.Id);

            var query = new GetEscalationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Should().ContainSingle();

            var dto = result.Value.Items.Single();

            dto.Id.Should().Be(request.Id);
            dto.TicketId.Should().Be(ticket.Id);
            dto.TicketTitle.Should().Be(ticket.Title);

            dto.RequestedByUserId.Should().Be(requester.Id);
            dto.RequestedByDisplayName.Should().Be(requester.DisplayName);

            dto.ProposedRecipientUserId.Should().Be(recipient.Id);
            dto.ProposedRecipientDisplayName.Should().Be(recipient.DisplayName);

            dto.Status.Should().Be(EscalationRequestStatus.Waiting);
            dto.RespondedAt.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenNoProposedRecipientExists_ReturnsNullRecipientDetails()
        {
            // Arrange
            var requester = await CreateUser("Requesting Agent");
            var ticket = await CreateTicket(requester.Id);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id);

            var query = new GetEscalationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var dto = result.Value.Items.Single();

            dto.ProposedRecipientUserId.Should().BeNull();
            dto.ProposedRecipientDisplayName.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenStatusFilterIsProvided_ReturnsOnlyMatchingStatuses()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Waiting);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Accepted);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Rejected);

            var query = new GetEscalationsQuery(
                Status: EscalationRequestStatus.Accepted);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle();

            result.Value.Items.Single().Status.Should().Be(EscalationRequestStatus.Accepted);
        }

        [Fact]
        public async Task Handle_WhenTicketIdFilterIsProvided_ReturnsOnlyMatchingTicketRequests()
        {
            // Arrange
            var requester = await CreateUser();

            var firstTicket = await CreateTicket(requester.Id, "First Ticket");
            var secondTicket = await CreateTicket(requester.Id, "Second Ticket");

            await CreateEscalationRequest(firstTicket.Id, requester.Id);
            await CreateEscalationRequest(secondTicket.Id, requester.Id);

            var query = new GetEscalationsQuery(
                TicketId: firstTicket.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle();

            result.Value.Items.Single().TicketId.Should().Be(firstTicket.Id);
        }

        [Fact]
        public async Task Handle_WhenRequestedByUserIdFilterIsProvided_ReturnsOnlyMatchingRequests()
        {
            // Arrange
            var firstRequester = await CreateUser("First Agent");
            var secondRequester = await CreateUser("Second Agent");

            var ticket = await CreateTicket(firstRequester.Id);

            await CreateEscalationRequest(ticket.Id, firstRequester.Id);
            await CreateEscalationRequest(ticket.Id, secondRequester.Id);

            var query = new GetEscalationsQuery(
                RequestedByUserId: firstRequester.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().ContainSingle();

            result.Value.Items.Single().RequestedByUserId.Should().Be(firstRequester.Id);
        }

        [Fact]
        public async Task Handle_WhenMultipleFiltersAreProvided_AppliesAllFilters()
        {
            // Arrange
            var firstRequester = await CreateUser("First Agent");
            var secondRequester = await CreateUser("Second Agent");

            var firstTicket = await CreateTicket(firstRequester.Id);
            var secondTicket = await CreateTicket(secondRequester.Id);

            await CreateEscalationRequest(
                firstTicket.Id,
                firstRequester.Id,
                status: EscalationRequestStatus.Waiting);

            await CreateEscalationRequest(
                firstTicket.Id,
                secondRequester.Id,
                status: EscalationRequestStatus.Waiting);

            await CreateEscalationRequest(
                secondTicket.Id,
                firstRequester.Id,
                status: EscalationRequestStatus.Waiting);

            var query = new GetEscalationsQuery(
                Status: EscalationRequestStatus.Waiting,
                TicketId: firstTicket.Id,
                RequestedByUserId: firstRequester.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.TotalCount.Should().Be(1);

            var dto = result.Value.Items.Single();

            dto.TicketId.Should().Be(firstTicket.Id);
            dto.RequestedByUserId.Should().Be(firstRequester.Id);
            dto.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public async Task Handle_WhenRequestsHaveDifferentStatuses_ReturnsWaitingRequestsFirst()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Accepted);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Waiting);

            await CreateEscalationRequest(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Rejected);

            var query = new GetEscalationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.First().Status.Should()
                .Be(EscalationRequestStatus.Waiting);

            result.Value.Items.Skip(1).Should()
                .OnlyContain(x => x.Status != EscalationRequestStatus.Waiting);
        }

        [Fact]
        public async Task Handle_WhenPageSizeIsProvided_ReturnsOnlyRequestedNumberOfItems()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            for (int i = 0; i < 5; i++)
                await CreateEscalationRequest(ticket.Id, requester.Id);

            var query = new GetEscalationsQuery(
                PageNumber: 1,
                PageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalPages.Should().Be(3);
            result.Value.HasNextPage.Should().BeTrue();
            result.Value.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WhenRequestingSecondPage_ReturnsCorrectPage()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            for (int i = 0; i < 5; i++)
                await CreateEscalationRequest(ticket.Id, requester.Id);

            var query = new GetEscalationsQuery(
                PageNumber: 2,
                PageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
            result.Value.HasNextPage.Should().BeTrue();
            result.Value.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenPageNumberIsLessThanOne_UsesFirstPage()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            await CreateEscalationRequest(ticket.Id, requester.Id);

            var query = new GetEscalationsQuery(PageNumber: 0);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageNumber.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WhenPageSizeIsLessThanOne_UsesDefaultPageSize()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            await CreateEscalationRequest(ticket.Id, requester.Id);

            var query = new GetEscalationsQuery(PageSize: 0);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WhenPageSizeExceedsMaximum_UsesMaximumPageSize()
        {
            // Arrange
            var requester = await CreateUser();
            var ticket = await CreateTicket(requester.Id);

            await CreateEscalationRequest(ticket.Id, requester.Id);

            var query = new GetEscalationsQuery(PageSize: 150);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageSize.Should().Be(100);
        }
    }
}
