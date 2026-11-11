using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class TicketTests
    {
        private static Ticket CreateValidTicket(
            string title = "Ticket title",
            string description = "Ticket description.",
            Guid? createdById = null,
            TicketPriority priority = TicketPriority.Medium)
        {
            return new Ticket(
                title,
                description,
                createdById ?? Guid.NewGuid(),
                priority);
        }

        [Fact]
        public void Constructor_WithValidDetails_ShouldCreateTicket()
        {
            // Arrange
            var title = "Ticket title";
            var description = "Ticket description.";
            var createdById = Guid.NewGuid();

            // Act
            var ticket = new Ticket(title, description, createdById);

            // Assert
            ticket.Id.Should().NotBeEmpty();
            ticket.Title.Should().Be(title);
            ticket.Description.Should().Be(description);
            ticket.CreatedById.Should().Be(createdById);
            ticket.Priority.Should().Be(TicketPriority.Medium);
            ticket.Status.Should().Be(TicketStatus.Open);
            ticket.AssignedToId.Should().BeNull();

            ticket.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            ticket.UpdatedAt.Should().Be(ticket.CreatedAt);

            ticket.Activities.Should().ContainSingle();
            ticket.Activities.Single().EventType.Should().Be(TicketActivityType.Created);
        }

        [Fact]
        public void Constructor_WithCustomPriority_ShouldUseSpecifiedPriority()
        {
            // Arrange
            var priority = Enum.GetValues<TicketPriority>().First(x => x != TicketPriority.Medium);

            // Act
            var ticket = CreateValidTicket(priority: priority);

            // Assert
            ticket.Priority.Should().Be(priority);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_WithMissingTitle_ShouldThrowDomainException(string title)
        {
            // Act
            Action act = () => CreateValidTicket(title: title);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Ticket title is required.");
        }

        [Fact]
        public void Constructor_WithTitleExceeding200Characters_ShouldThrowDomainException()
        {
            // Arrange
            var title = new string('a', 201);

            // Act
            Action act = () => CreateValidTicket(title: title);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Ticket title cannot exceed 200 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_WithMissingDescription_ShouldThrowDomainException(string description)
        {
            // Act
            Action act = () => CreateValidTicket(description: description);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Ticket description is required.");
        }

        [Fact]
        public void Constructor_WithDescriptionExceeding5000Characters_ShouldThrowDomainException()
        {
            // Arrange
            var description = new string('a', 5001);

            // Act
            Action act = () => CreateValidTicket(description: description);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Ticket description cannot exceed 5000 characters.");
        }

        [Fact]
        public void Constructor_WithEmptyCreatorId_ShouldThrowDomainException()
        {
            // Act
            Action act = () => CreateValidTicket(createdById: Guid.Empty);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A ticket must specify a creator.");
        }

        [Fact]
        public void Constructor_WithValidDetails_ShouldTrimTitleAndDescription()
        {
            // Act
            var ticket = CreateValidTicket(
                title: "  Ticket title  ",
                description: "  Ticket description  ");

            // Assert
            ticket.Title.Should().Be("Ticket title");
            ticket.Description.Should().Be("Ticket description");
        }



        [Fact]
        public void Claim_WithValidDetails_ShouldAssignTicket()
        {
            // Arrange
            var ticket = CreateValidTicket();
            var agentId = Guid.NewGuid();
            var displayName = "Support Agent";

            // Act
            ticket.Claim(agentId, displayName);

            // Assert
            ticket.AssignedToId.Should().Be(agentId);
            ticket.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            ticket.Activities.Should().ContainSingle(x => x.EventType == TicketActivityType.Assigned);
        }

        [Fact]
        public void Claim_WithEmptyAgentId_ShouldThrowDomainException()
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            Action act = () => ticket.Claim(Guid.Empty, "Support Agent");

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A valid user ID must be specified to assign the ticket.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Claim_WithMissingDisplayName_ShouldThrowDomainException(string displayName)
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            Action act = () => ticket.Claim(Guid.NewGuid(), displayName);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A valid display name must be specified.");
        }

        [Fact]
        public void Claim_WhenAlreadyAssigned_ShouldThrowDomainException()
        {
            // Arrange
            var ticket = CreateValidTicket();
            ticket.Claim(Guid.NewGuid(), "Agent One");

            // Act
            Action act = () => ticket.Claim(Guid.NewGuid(), "Agent Two");

            // Assert
            act.Should().Throw<DomainException>().WithMessage("This ticket has already been assigned.");
        }

        [Fact]
        public void AssignTo_WhenUnassigned_ShouldAssignTicket()
        {
            // Arrange
            var ticket = CreateValidTicket();
            var assignerId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();

            // Act
            ticket.AssignTo(
                assignerId,
                "Administrator",
                assigneeId,
                "Support Agent",
                null);

            // Assert
            ticket.AssignedToId.Should().Be(assigneeId);
            ticket.Activities.Should().ContainSingle(x => x.EventType == TicketActivityType.Assigned);
        }

        [Fact]
        public void AssignTo_WhenAlreadyAssigned_ShouldReassignTicket()
        {
            // Arrange
            var ticket = CreateValidTicket();
            var oldAssigneeId = Guid.NewGuid();
            var newAssigneeId = Guid.NewGuid();

            ticket.Claim(oldAssigneeId, "Old Agent");

            // Act
            ticket.AssignTo(
                Guid.NewGuid(),
                "Administrator",
                newAssigneeId,
                "New Agent",
                "Old Agent");

            // Assert
            ticket.AssignedToId.Should().Be(newAssigneeId);
            ticket.Activities.Should().HaveCount(3);
        }

        [Fact]
        public void AssignTo_WhenAlreadyAssignedToSameAgent_ShouldDoNothing()
        {
            // Arrange
            var ticket = CreateValidTicket();
            var agentId = Guid.NewGuid();

            ticket.Claim(agentId, "Support Agent");

            var previousUpdatedAt = ticket.UpdatedAt;
            var previousActivityCount = ticket.Activities.Count;

            // Act
            ticket.AssignTo(
                Guid.NewGuid(),
                "Administrator",
                agentId,
                "Support Agent",
                null);

            // Assert
            ticket.AssignedToId.Should().Be(agentId);
            ticket.UpdatedAt.Should().Be(previousUpdatedAt);
            ticket.Activities.Should().HaveCount(previousActivityCount);
        }

        [Fact]
        public void AssignTo_WithEmptyAssignerId_ShouldThrowDomainException()
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            Action act = () => ticket.AssignTo(
                Guid.Empty, "Administrator",
                Guid.NewGuid(), "Support Agent", null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A valid user ID must be specified to assign the ticket.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void AssignTo_WithMissingAssignerDisplayName_ShouldThrowDomainException(string displayName)
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            Action act = () => ticket.AssignTo(
                Guid.NewGuid(), displayName,
                Guid.NewGuid(), "Support Agent", null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A valid assigner display name must be specified.");
        }

        [Fact]
        public void AssignTo_WithEmptyAssigneeId_ShouldThrowDomainException()
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            Action act = () => ticket.AssignTo(
                Guid.NewGuid(), "Administrator",
                Guid.Empty, "Support Agent", null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A valid user ID must be specified to assign the ticket to.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void AssignTo_WithMissingAssigneeDisplayName_ShouldThrowDomainException(string displayName)
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            Action act = () => ticket.AssignTo(
                Guid.NewGuid(), "Administrator",
                Guid.NewGuid(), displayName, null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A valid assignee display name must be specified.");
        }

        [Fact]
        public void Unassign_WhenAssigned_ShouldRemoveAssignment()
        {
            // Arrange
            var ticket = CreateValidTicket();
            ticket.Claim(Guid.NewGuid(), "Support Agent");

            // Act
            ticket.Unassign();

            // Assert
            ticket.AssignedToId.Should().BeNull();
            ticket.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Unassign_WhenAlreadyUnassigned_ShouldRemainUnassigned()
        {
            // Arrange
            var ticket = CreateValidTicket();

            // Act
            ticket.Unassign();

            // Assert
            ticket.AssignedToId.Should().BeNull();
        }

        [Fact]
        public void ChangePriority_WithDifferentPriority_ShouldUpdatePriority()
        {
            // Arrange
            var ticket = CreateValidTicket();
            var newPriority = Enum.GetValues<TicketPriority>()
                .First(x => x != ticket.Priority);

            var agentId = Guid.NewGuid();

            // Act
            ticket.ChangePriority(newPriority, agentId);

            // Assert
            ticket.Priority.Should().Be(newPriority);
            ticket.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            ticket.Activities.Should().ContainSingle(x => x.EventType == TicketActivityType.PriorityChanged);
        }

        [Fact]
        public void ChangePriority_WithSamePriority_ShouldDoNothing()
        {
            // Arrange
            var ticket = CreateValidTicket();
            var previousUpdatedAt = ticket.UpdatedAt;
            var previousActivityCount = ticket.Activities.Count;

            // Act
            ticket.ChangePriority(ticket.Priority, Guid.NewGuid());

            // Assert
            ticket.Priority.Should().Be(TicketPriority.Medium);
            ticket.UpdatedAt.Should().Be(previousUpdatedAt);
            ticket.Activities.Should().HaveCount(previousActivityCount);
        }
    }
}