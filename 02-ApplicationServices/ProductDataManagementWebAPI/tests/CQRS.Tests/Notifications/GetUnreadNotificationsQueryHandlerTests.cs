using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Notifications;
using CQRS.Notifications.GetUnreadNotifications;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Notifications;

public sealed class GetUnreadNotificationsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetUnreadNotificationsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetUnreadNotificationsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);
        _handler = new GetUnreadNotificationsQueryHandler(_notificationRepoMock.Object, _currentUserMock.Object);
    }

    private static Notification BuildUnreadNotification(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TenantId = Guid.NewGuid(),
        Title = "Unread notification",
        Message = "Content",
        IsRead = false,
        Tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant" }
    };

    [Fact]
    public async Task Handle_WhenUnreadNotificationsExist_ReturnsMappedItems()
    {
        // Arrange
        List<Notification> notifications = new()
        {
            BuildUnreadNotification(_userId),
            BuildUnreadNotification(_userId),
            BuildUnreadNotification(_userId)
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

        GetUnreadNotificationsQuery query = new() { Take = 50, Skip = 0 };

        // Act
        IEnumerable<NotificationWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(n => !n.IsRead);
    }

    [Fact]
    public async Task Handle_WhenNoUnreadNotifications_ReturnsEmpty()
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

        GetUnreadNotificationsQuery query = new() { Take = 50, Skip = 0 };

        // Act
        IEnumerable<NotificationWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
