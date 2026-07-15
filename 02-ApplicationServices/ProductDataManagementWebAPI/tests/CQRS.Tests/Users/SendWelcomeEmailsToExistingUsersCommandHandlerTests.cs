using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using CQRS.Users.SendWelcomeEmailsToExistingUsers;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Users;

public sealed class SendWelcomeEmailsToExistingUsersCommandHandlerTests
{
    private readonly Mock<IReadRepository<User>> _userReadRepoMock = new();
    private readonly Mock<IRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IWelcomeEmailService> _welcomeEmailServiceMock = new();
    private readonly SendWelcomeEmailsToExistingUsersCommandHandler _handler;

    public SendWelcomeEmailsToExistingUsersCommandHandlerTests()
    {
        _welcomeEmailServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendWelcomeEmailsToExistingUsersCommandHandler(
            _userReadRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object,
            _welcomeEmailServiceMock.Object,
            NullLogger<SendWelcomeEmailsToExistingUsersCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);
        SendWelcomeEmailsToExistingUsersCommand command = new();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_SendsToUsersWithoutSentAt()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        User user1 = BuildUser("user1@test.com");
        User user2 = BuildUser("user2@test.com");

        _userReadRepoMock
            .SetupSequence(r => r.GetPagedBySearchAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Expression<Func<User, DateTime>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync([user1, user2])
            .ReturnsAsync(new List<User>());

        SendWelcomeEmailsToExistingUsersCommand command = new();

        // Act
        SendWelcomeEmailsResultWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SentCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        user1.WelcomeEmailSentAt.Should().NotBeNull();
        user2.WelcomeEmailSentAt.Should().NotBeNull();
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenNoPendingUsers_ReturnsZeroSent()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        _userReadRepoMock
            .Setup(r => r.GetPagedBySearchAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Expression<Func<User, DateTime>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(new List<User>());

        SendWelcomeEmailsToExistingUsersCommand command = new();

        // Act
        SendWelcomeEmailsResultWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SentCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static User BuildUser(string email) => new User
    {
        Id = Guid.NewGuid(),
        Email = email,
        FirstName = "Jan",
        LastName = "Kowalski",
        IsActive = true,
        AzureAdB2CObjectId = Guid.NewGuid().ToString()
    };
}
