using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Notifications.MarkNotificationAsRead;
using Entities.Models.Notifications;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Notifications;

public sealed class MarkNotificationAsReadCommandHandlerTests
{
    private readonly Mock<IRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<INotificationMarkAsReadSender> _senderMock = new();
    private readonly MarkNotificationAsReadCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);
        _currentUserMock.Setup(u => u.AzureAdB2CObjectId).Returns("oid-abc");

        _senderMock
            .Setup(s => s.EnqueueAsync(It.IsAny<NotificationMarkAsReadDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new MarkNotificationAsReadCommandHandler(
            _notificationRepoMock.Object,
            _currentUserMock.Object,
            _senderMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotificationExists_MarksAsReadAndReturnsUnit()
    {
        // Arrange
        Guid notificationId = Guid.NewGuid();
        Notification notification = new()
        {
            Id = notificationId,
            UserId = _userId,
            IsRead = false,
            TenantId = Guid.NewGuid(),
            Title = "Test"
        };

        _notificationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>[]>()))
            .ReturnsAsync(notification);

        MarkNotificationAsReadCommand command = new() { NotificationId = notificationId };

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        notification.IsRead.Should().BeTrue();
        _notificationRepoMock.Verify(r => r.Update(notification), Times.Once);
        _notificationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationMarkAsReadDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>[]>()))
            .ReturnsAsync((Notification?)null);

        MarkNotificationAsReadCommand command = new() { NotificationId = Guid.NewGuid() };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNotificationAlreadyRead_ReturnsUnitWithoutUpdating()
    {
        // Arrange
        Guid notificationId = Guid.NewGuid();
        Notification notification = new()
        {
            Id = notificationId,
            UserId = _userId,
            IsRead = true,
            TenantId = Guid.NewGuid()
        };

        _notificationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>[]>()))
            .ReturnsAsync(notification);

        MarkNotificationAsReadCommand command = new() { NotificationId = notificationId };

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _notificationRepoMock.Verify(r => r.Update(It.IsAny<Notification>()), Times.Never);
        _senderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationMarkAsReadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
