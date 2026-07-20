using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.Users.SendWelcomeEmailToUser;
using Entities.Enums;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Admin;

public sealed class SendWelcomeEmailToUserCommandHandlerTests
{
    private readonly Mock<IReadRepository<User>> _userReadRepoMock = new();
    private readonly Mock<IRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IWelcomeEmailService> _welcomeEmailServiceMock = new();
    private readonly SendWelcomeEmailToUserCommandHandler _handler;

    public SendWelcomeEmailToUserCommandHandlerTests()
    {
        _welcomeEmailServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendWelcomeEmailToUserCommandHandler(
            _userReadRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object,
            _welcomeEmailServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(
            new SendWelcomeEmailToUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);
        _userReadRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(
            new SendWelcomeEmailToUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_SendsEmailAndUpdatesSentAt()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);
        Guid userId = Guid.NewGuid();
        User user = new()
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "Jan",
            LastName = "Kowalski",
            IsActive = true,
            SystemRole = SystemRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            WelcomeEmailSentAt = null
        };

        _userReadRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(user);

        // Act
        AdminUserWeb result = await _handler.Handle(
            new SendWelcomeEmailToUserCommand(userId),
            CancellationToken.None);

        // Assert
        result.Id.Should().Be(userId);
        result.WelcomeEmailSentAt.Should().NotBeNull();
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
    }
}
