using Business.Implementation.Services.Files;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models.Notifications;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services.Files;

public class FileShareNotificationServiceTests
{
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ILogger<FileShareNotificationService>> _loggerMock = new();
    private readonly FileShareNotificationService _sut;

    public FileShareNotificationServiceTests()
    {
        _sut = new FileShareNotificationService(
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static FileShareNotificationContext BuildContext(IReadOnlyCollection<Guid> userIds)
        => new FileShareNotificationContext
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            FileDisplayName = "report.pdf",
            OwnerName = "Jan Kowalski",
            UserIds = userIds
        };

    private static User BuildUser(Guid userId, string firstName = "Anna", string lastName = "Nowak")
        => new User
        {
            Id = userId,
            Email = "anna@test.com",
            FirstName = firstName,
            LastName = lastName,
            AzureAdB2CObjectId = Guid.NewGuid().ToString()
        };

    // ─── NotifyShareGrantedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task NotifyShareGrantedAsync_EmptyUserIds_DoesNotEnqueueNotification()
    {
        // Arrange
        FileShareNotificationContext context = BuildContext(Array.Empty<Guid>());

        // Act
        await _sut.NotifyShareGrantedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyShareGrantedAsync_UserFound_EnqueuesNotification()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = BuildUser(userId);
        FileShareNotificationContext context = BuildContext(new[] { userId });

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _sut.NotifyShareGrantedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyShareGrantedAsync_UserNotInRepo_SkipsNotification()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        FileShareNotificationContext context = BuildContext(new[] { userId });

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>()); // user not found

        // Act
        await _sut.NotifyShareGrantedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyShareGrantedAsync_MultipleUsers_EnqueuesForEach()
    {
        // Arrange
        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        List<User> users = new List<User>
        {
            BuildUser(userId1, "Jan", "Nowak"),
            BuildUser(userId2, "Maria", "Kowalska")
        };
        FileShareNotificationContext context = BuildContext(new[] { userId1, userId2 });

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(users);

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        await _sut.NotifyShareGrantedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ─── NotifyShareRevokedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task NotifyShareRevokedAsync_UserFound_EnqueuesNotification()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = BuildUser(userId);
        FileShareNotificationContext context = BuildContext(new[] { userId });

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.NotifyShareRevokedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyShareGrantedAsync_RepoThrows_ExceptionSwallowed()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        FileShareNotificationContext context = BuildContext(new[] { userId });

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        Func<Task> act = async () => await _sut.NotifyShareGrantedAsync(context, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
