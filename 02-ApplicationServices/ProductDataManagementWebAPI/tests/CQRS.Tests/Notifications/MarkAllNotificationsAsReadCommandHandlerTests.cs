using Business.Interfaces.Model;
using CQRS.Notifications.MarkAllNotificationsAsRead;
using Entities.Models.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Notifications;

public sealed class MarkAllNotificationsAsReadCommandHandlerTests
{
    private readonly Mock<IRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<MarkAllNotificationsAsReadCommandHandler>> _loggerMock = new();
    private readonly MarkAllNotificationsAsReadCommandHandler _handler;

    public MarkAllNotificationsAsReadCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new MarkAllNotificationsAsReadCommandHandler(
            _notificationRepoMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUnreadNotificationsExist_ReturnsUpdatedCount()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<Notification>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        MarkAllNotificationsAsReadCommand command = new();

        // Act
        int result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenNoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<Notification>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        MarkAllNotificationsAsReadCommand command = new();

        // Act
        int result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }
}
