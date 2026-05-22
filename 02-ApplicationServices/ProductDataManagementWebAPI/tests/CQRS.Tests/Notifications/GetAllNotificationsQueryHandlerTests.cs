using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Notifications;
using CQRS.Notifications.GetAllNotifications;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Notifications;

public sealed class GetAllNotificationsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<GetAllNotificationsQueryHandler>> _loggerMock = new();
    private readonly GetAllNotificationsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetAllNotificationsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _handler = new GetAllNotificationsQueryHandler(
            _notificationRepoMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private static Notification BuildNotification(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TenantId = Guid.NewGuid(),
        Title = "Test notification",
        Message = "Test message",
        IsRead = false,
        Tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant" }
    };

    [Fact]
    public async Task Handle_WhenNotificationsExist_ReturnsMappedItems()
    {
        // Arrange
        List<Notification> notifications = new()
        {
            BuildNotification(_userId),
            BuildNotification(_userId)
        };

        _notificationRepoMock
            .Setup(r => r.GetPagedBySearchAsync<DateTime>(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Expression<Func<Notification, DateTime>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>[]>()))
            .ReturnsAsync(notifications);

        GetAllNotificationsQuery query = new() { Take = 50, Skip = 0 };

        // Act
        IEnumerable<NotificationWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenNoNotifications_ReturnsEmpty()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.GetPagedBySearchAsync<DateTime>(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Expression<Func<Notification, DateTime>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>[]>()))
            .ReturnsAsync(new List<Notification>());

        GetAllNotificationsQuery query = new() { Take = 50, Skip = 0 };

        // Act
        IEnumerable<NotificationWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
