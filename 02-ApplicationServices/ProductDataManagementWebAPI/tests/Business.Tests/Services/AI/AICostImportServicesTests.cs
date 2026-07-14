using Business.Implementation.Services.AI;
using Business.Interfaces.DTO;
using NotifType = Business.Interfaces.DTO.NotificationType;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Enums;
using Entities.Models.AI;
using Entities.Models.Costs;
using Entities.Models.Notifications;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace Business.Tests.Services.AI;

public sealed class AICostDuplicateDetectionServiceTests
{
    private readonly Mock<IReadRepository<AICostImportItem>> _importItemRepoMock = new();
    private readonly Mock<IReadRepository<BaseCost>> _costRepoMock = new();
    private readonly AICostDuplicateDetectionService _sut;

    public AICostDuplicateDetectionServiceTests()
    {
        _sut = new AICostDuplicateDetectionService(
            _importItemRepoMock.Object,
            _costRepoMock.Object);
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenHashExistsInItems_ReturnsTrue()
    {
        // Arrange
        ParsedCostDto parsed = BuildParsedData();

        _importItemRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _sut.IsDuplicateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hash123",
            parsed,
            null,
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _costRepoMock.Verify(
            r => r.AnyAsync(
                It.IsAny<Expression<Func<BaseCost, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenHashExistsInCosts_ReturnsTrue()
    {
        // Arrange
        ParsedCostDto parsed = BuildParsedData();

        _importItemRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _costRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<BaseCost, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _sut.IsDuplicateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hash123",
            parsed,
            null,
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenNoMatch_ReturnsFalse()
    {
        // Arrange
        ParsedCostDto parsed = BuildParsedData();

        _importItemRepoMock
            .SetupSequence(r => r.AnyAsync(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(false);

        _costRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<BaseCost, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _costRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<BaseCost, bool>>>()))
            .ReturnsAsync(new List<BaseCost>());

        // Act
        bool result = await _sut.IsDuplicateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "unique-hash",
            parsed,
            null,
            CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    private static ParsedCostDto BuildParsedData() =>
        new ParsedCostDto
        {
            Name = "Test",
            Net = 100m,
            Number = "FV/1",
            Date = new DateTime(2026, 1, 1),
            ContractorId = Guid.NewGuid(),
            Confidence = 0.9
        };
}

public sealed class AICostImportNotificationServiceTests
{
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ILogger<AICostImportNotificationService>> _loggerMock = new();
    private readonly AICostImportNotificationService _sut;

    public AICostImportNotificationServiceTests()
    {
        _sut = new AICostImportNotificationService(
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyBatchCompletedAsync_WhenUserNotFound_DoesNotEnqueue()
    {
        // Arrange
        AICostImportBatch batch = BuildBatch();

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _sut.NotifyBatchCompletedAsync(batch, CancellationToken.None);

        // Assert
        _notificationSenderMock.Verify(
            s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyBatchCompletedAsync_WhenUserFound_EnqueuesNotificationWithRoute()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        AICostImportBatch batch = BuildBatch(userId, projectId);

        User user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "Jan",
            LastName = "Kowalski",
            AzureAdB2CObjectId = Guid.NewGuid().ToString()
        };

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        NotificationPayloadDto? capturedPayload = null;
        _notificationSenderMock
            .Setup(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationPayloadDto, CancellationToken>((payload, _) => capturedPayload = payload)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.NotifyBatchCompletedAsync(batch, CancellationToken.None);

        // Assert
        capturedPayload.Should().NotBeNull();
        capturedPayload!.Notification.Metadata.Should().ContainKey("route");
        capturedPayload.Notification.Metadata!["route"]
            .Should().Be($"/projects/{projectId}/costs/ai-review");
        capturedPayload.UnreadNotificationCounter.Should().Be(3);
    }

    [Fact]
    public async Task NotifyBatchCompletedAsync_WhenErrorsPresent_UsesWarningType()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        AICostImportBatch batch = BuildBatch(userId, Guid.NewGuid());
        batch.ErrorCount = 1;

        User user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "Jan",
            LastName = "Kowalski",
            AzureAdB2CObjectId = Guid.NewGuid().ToString()
        };

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        NotificationPayloadDto? capturedPayload = null;
        _notificationSenderMock
            .Setup(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationPayloadDto, CancellationToken>((payload, _) => capturedPayload = payload)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.NotifyBatchCompletedAsync(batch, CancellationToken.None);

        // Assert
        capturedPayload.Should().NotBeNull();
        capturedPayload!.Notification.Type.Should().Be(NotifType.Warning);
    }

    private static AICostImportBatch BuildBatch(Guid? userId = null, Guid? projectId = null) =>
        new AICostImportBatch
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            CreatedByUserId = userId ?? Guid.NewGuid(),
            CostDocumentType = CostDocumentType.ProjectCost,
            Status = AICostImportBatchStatus.Completed,
            TotalFiles = 3,
            ProcessedFiles = 3,
            PendingCount = 2,
            ErrorCount = 0,
            DuplicateCount = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
