using Business.Interfaces.Model;
using CQRS.Notifications.GetUnreadCounter;
using Entities.Models.Notifications;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Notifications;

public sealed class GetUnreadCounterQueryHandlerTests
{
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetUnreadCounterQueryHandler _handler;

    public GetUnreadCounterQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _handler = new GetUnreadCounterQueryHandler(_notificationRepoMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUnreadNotificationsExist_ReturnsCount()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        GetUnreadCounterQuery query = new();

        // Act
        int result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenNoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        GetUnreadCounterQuery query = new();

        // Act
        int result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }
}
