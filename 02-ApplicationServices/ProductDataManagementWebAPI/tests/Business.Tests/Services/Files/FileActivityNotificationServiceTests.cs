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

public sealed class FileActivityNotificationServiceTests
{
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ILogger<FileActivityNotificationService>> _loggerMock = new();
    private readonly FileActivityNotificationService _sut;

    public FileActivityNotificationServiceTests()
    {
        _sut = new FileActivityNotificationService(
            _projectFilesServiceMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _loggerMock.Object);
    }

    private static FileActivityNotificationContext BuildContext(
        Guid ownerId,
        Guid actorUserId,
        Guid fileId)
        => new FileActivityNotificationContext
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = fileId,
            PackageId = Guid.NewGuid(),
            OwnerId = ownerId,
            FileDisplayName = "report.pdf",
            ActorName = "Jan Kowalski",
            ActorUserId = actorUserId,
            VersionId = Guid.NewGuid(),
        };

    private static User BuildUser(Guid userId)
        => new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "Anna",
            LastName = "Nowak",
            AzureAdB2CObjectId = Guid.NewGuid().ToString()
        };

    private void SetupSharedUsers(Guid fileId, params Guid[] sharedUserIds)
    {
        Dictionary<Guid, List<Guid>> shared = new Dictionary<Guid, List<Guid>>();
        if (sharedUserIds.Length > 0)
        {
            shared[fileId] = sharedUserIds.ToList();
        }

        _projectFilesServiceMock
            .Setup(s => s.GetSharedWithUsersAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(shared);
    }

    private void SetupUsers(params User[] users)
    {
        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(users.ToList());

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    [Fact]
    public async Task NotifyVersionUploadedAsync_NotifiesOwnerAndSharedUsers_ExcludesActor()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid sharedId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        FileActivityNotificationContext context = BuildContext(ownerId, actorId, fileId);

        SetupSharedUsers(fileId, sharedId, actorId);
        SetupUsers(BuildUser(ownerId), BuildUser(sharedId), BuildUser(actorId));

        // Act
        await _sut.NotifyVersionUploadedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(
                It.Is<NotificationPayloadDto>(p =>
                    p.Notification.Title == "Nowa wersja pliku"
                    && (p.Notification.UserId == ownerId || p.Notification.UserId == sharedId)),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(
                It.Is<NotificationPayloadDto>(p => p.Notification.UserId == actorId),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyCommentAddedAsync_WhenActorIsOwner_NotifiesOnlySharedUsers()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid sharedId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        FileActivityNotificationContext context = BuildContext(ownerId, actorUserId: ownerId, fileId);

        SetupSharedUsers(fileId, sharedId);
        SetupUsers(BuildUser(sharedId));

        // Act
        await _sut.NotifyCommentAddedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(
                It.Is<NotificationPayloadDto>(p =>
                    p.Notification.Title == "Nowy komentarz do pliku"
                    && p.Notification.UserId == sharedId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyCommentAddedAsync_NoRecipients_DoesNotEnqueue()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        FileActivityNotificationContext context = BuildContext(ownerId, actorUserId: ownerId, fileId);

        SetupSharedUsers(fileId);
        SetupUsers();

        // Act
        await _sut.NotifyCommentAddedAsync(context, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyVersionUploadedAsync_ServiceThrows_ExceptionSwallowed()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        FileActivityNotificationContext context = BuildContext(ownerId, actorId, fileId);

        _projectFilesServiceMock
            .Setup(s => s.GetSharedWithUsersAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cache error"));

        // Act
        Func<Task> act = async () => await _sut.NotifyVersionUploadedAsync(context, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyCommentAddedAsync_IncludesDeepLinkWithCommentId()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        Guid packageId = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        Guid commentId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        FileActivityNotificationContext context = new FileActivityNotificationContext
        {
            TenantId = Guid.NewGuid(),
            ProjectId = projectId,
            FileId = fileId,
            PackageId = packageId,
            OwnerId = ownerId,
            FileDisplayName = "report.pdf",
            ActorName = "Jan Kowalski",
            ActorUserId = actorId,
            VersionId = versionId,
            CommentId = commentId,
        };

        SetupSharedUsers(fileId);
        SetupUsers(BuildUser(ownerId));

        NotificationPayloadDto? captured = null;
        _notificationSenderMock
            .Setup(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationPayloadDto, CancellationToken>((payload, _) => captured = payload)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.NotifyCommentAddedAsync(context, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        string? route = captured!.Notification.Metadata?["route"]?.ToString();
        route.Should().Contain($"/projects/{projectId}/files");
        route.Should().Contain($"fileId={fileId}");
        route.Should().Contain($"packageId={packageId}");
        route.Should().Contain($"versionId={versionId}");
        route.Should().Contain($"commentId={commentId}");
        captured.Notification.Metadata?["CommentId"]?.ToString().Should().Be(commentId.ToString());
    }
}
