using FluentAssertions;
using Helpdesk.Api.Features.Auth.GetCurrentUser;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Auth.GetCurrentUser
{
    public class GetCurrentUserQueryHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly GetCurrentUserQueryHandler _handler;

        public GetCurrentUserQueryHandlerTests()
        {
            _userManagerMock = IdentityMockFactory.CreateUserManager();

            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _handler = new GetCurrentUserQueryHandler(
                _userManagerMock.Object);
        }

        private ApplicationUser CreateUser(string displayName = "Test User")
        {
            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = displayName
            };

            return newUser;
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var command = new GetCurrentUserQuery(Guid.NewGuid());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("User.NotFound");
        }

        [Fact]
        public async Task Handle_WhenUserDoesExist_ReturnsCurrentUserDetails()
        {
            // Arrange
            var user = CreateUser("Test User");

            var command = new GetCurrentUserQuery(Guid.NewGuid());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(["SupportAgent"]);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(user.Id);
            result.Value.Email.Should().Be(user.Email);
            result.Value.DisplayName.Should().Be(user.DisplayName);
            result.Value.Roles.Should().ContainSingle();
            result.Value.Roles[0].Should().Be("SupportAgent");
        }
    }
}
