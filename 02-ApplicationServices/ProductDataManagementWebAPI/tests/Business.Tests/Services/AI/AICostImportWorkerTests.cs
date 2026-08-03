using System.Reflection;
using System.Text.Json;
using Business.Implementation.Services.AI;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Enums;
using Entities.Models.AI;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace Business.Tests.Services.AI;

public sealed class AICostImportWorkerTests
{
    private readonly Mock<IRepository<AICostImportItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<AICostImportBatch>> _batchRepoMock = new();
    private readonly Mock<IDocumentParserService> _parserMock = new();
    private readonly Mock<IAICostDocumentEnrichmentService> _enrichmentMock = new();
    private readonly Mock<IAICostDuplicateDetectionService> _duplicateMock = new();
    private readonly Mock<IAICostImportBlobService> _blobMock = new();
    private readonly Mock<IAICostImportNotificationService> _notificationMock = new();
    private readonly Mock<IQueueStorageService> _queueMock = new();
    private readonly AICostImportOptions _options = new AICostImportOptions
    {
        MaxRetryAttempts = 3,
        QueueName = "ai-cost-import-process",
        WorkerPollIntervalSeconds = 1
    };

    [Fact]
    public async Task ProcessMessage_WhenPdfPasswordProtected_SetsErrorNeedsReviewWithoutRequeue()
    {
        // Arrange
        Guid batchId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        AICostImportBatch batch = BuildBatch(batchId, totalFiles: 2, processedFiles: 0);
        AICostImportItem item = BuildItem(batchId, itemId, AICostImportItemStatus.Queued);

        SetupRepos(item, batch);
        SetupBlobDownload(item.BlobPath, [0x25, 0x50, 0x44, 0x46]);

        PdfConversionException conversionEx = PdfConversionException.PasswordProtected();
        _parserMock
            .Setup(p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(conversionEx);

        AICostImportWorker worker = CreateWorker();
        DequeuedMessage message = BuildDequeuedMessage(batchId, itemId);

        // Act
        await InvokeProcessMessageAsync(worker, message);

        // Assert
        item.Status.Should().Be(AICostImportItemStatus.ErrorNeedsReview);
        item.LastError.Should().Be(conversionEx.UserMessage);
        item.RetryCount.Should().Be(0);
        batch.ErrorCount.Should().Be(1);
        batch.ProcessedFiles.Should().Be(1);

        _queueMock.Verify(
            q => q.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _blobMock.Verify(
            b => b.UploadPendingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMessage_WhenZeroConfidenceThenSuccessOnRetry_CompletesItem()
    {
        // Arrange
        Guid batchId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        AICostImportBatch batch = BuildBatch(batchId, totalFiles: 1, processedFiles: 0);
        AICostImportItem item = BuildItem(batchId, itemId, AICostImportItemStatus.Queued);

        SetupRepos(item, batch);
        SetupBlobDownload(item.BlobPath, [0xFF, 0xD8, 0xFF]);

        ParsedCostDto zeroConfidence = new ParsedCostDto
        {
            Name = "Nieznany koszt",
            Confidence = 0
        };

        ParsedCostDto success = new ParsedCostDto
        {
            Name = "Materiały budowlane",
            Confidence = 0.9
        };

        int callCount = 0;
        _parserMock
            .Setup(p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? zeroConfidence : success;
            });

        _enrichmentMock
            .Setup(e => e.EnrichWithContractorAsync(
                It.IsAny<ParsedCostDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParsedCostDto dto, Guid _, CancellationToken _) => dto);

        _enrichmentMock
            .Setup(e => e.EnrichWithCategoryAsync(
                It.IsAny<ParsedCostDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParsedCostDto dto, Guid _, CancellationToken _) => dto);

        _duplicateMock
            .Setup(d => d.IsDuplicateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<ParsedCostDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        AICostImportWorker worker = CreateWorker();
        DequeuedMessage message = BuildDequeuedMessage(batchId, itemId);

        // Act
        await InvokeProcessMessageAsync(worker, message);

        // Assert
        item.Status.Should().Be(AICostImportItemStatus.Pending);
        item.RetryCount.Should().Be(0);
        callCount.Should().Be(2);
        batch.PendingCount.Should().Be(1);

        _queueMock.Verify(
            q => q.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMessage_WhenZeroConfidenceExhaustsRetries_RequeuesItem()
    {
        // Arrange
        Guid batchId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        AICostImportBatch batch = BuildBatch(batchId, totalFiles: 1, processedFiles: 0);
        AICostImportItem item = BuildItem(batchId, itemId, AICostImportItemStatus.Queued);

        SetupRepos(item, batch);
        SetupBlobDownload(item.BlobPath, [0xFF, 0xD8, 0xFF]);

        _parserMock
            .Setup(p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedCostDto
            {
                Name = "Nieznany koszt",
                Confidence = 0
            });

        AICostImportWorker worker = CreateWorker();
        DequeuedMessage message = BuildDequeuedMessage(batchId, itemId);

        // Act
        await InvokeProcessMessageAsync(worker, message);

        // Assert
        item.Status.Should().Be(AICostImportItemStatus.Queued);
        item.RetryCount.Should().Be(1);
        item.LastError.Should().Be("Document parser returned zero confidence.");

        _parserMock.Verify(
            p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(_options.MaxRetryAttempts));

        _queueMock.Verify(
            q => q.EnqueueAsync(
                _options.QueueName,
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WhenTransientFailure_RequeuesItem()
    {
        // Arrange
        Guid batchId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        AICostImportBatch batch = BuildBatch(batchId, totalFiles: 1, processedFiles: 0);
        AICostImportItem item = BuildItem(batchId, itemId, AICostImportItemStatus.Queued);

        SetupRepos(item, batch);
        SetupBlobDownload(item.BlobPath, [0xFF, 0xD8, 0xFF]);

        _parserMock
            .Setup(p => p.ParseAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI timeout"));

        AICostImportWorker worker = CreateWorker();
        DequeuedMessage message = BuildDequeuedMessage(batchId, itemId);

        // Act
        await InvokeProcessMessageAsync(worker, message);

        // Assert
        item.Status.Should().Be(AICostImportItemStatus.Queued);
        item.RetryCount.Should().Be(1);
        item.LastError.Should().Be("AI timeout");
        batch.ErrorCount.Should().Be(0);

        _queueMock.Verify(
            q => q.EnqueueAsync(
                _options.QueueName,
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AICostImportWorker CreateWorker()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(_itemRepoMock.Object);
        services.AddSingleton(_batchRepoMock.Object);
        services.AddSingleton(_parserMock.Object);
        services.AddSingleton(_enrichmentMock.Object);
        services.AddSingleton(_duplicateMock.Object);
        services.AddSingleton(_blobMock.Object);
        services.AddSingleton(_notificationMock.Object);

        return new AICostImportWorker(
            services.BuildServiceProvider(),
            _queueMock.Object,
            Options.Create(_options),
            NullLogger<AICostImportWorker>.Instance);
    }

    private void SetupRepos(AICostImportItem item, AICostImportBatch batch)
    {
        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(item);

        _batchRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportBatch, bool>>>()))
            .ReturnsAsync(batch);

        _itemRepoMock
            .Setup(r => r.Update(It.IsAny<AICostImportItem>()))
            .Returns(Task.CompletedTask);

        _batchRepoMock
            .Setup(r => r.Update(It.IsAny<AICostImportBatch>()))
            .Returns(Task.CompletedTask);

        _itemRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _batchRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private void SetupBlobDownload(string blobPath, byte[] content)
    {
        _blobMock
            .Setup(b => b.DownloadPendingAsync(blobPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobDownload
            {
                Content = new MemoryStream(content),
                ContentType = "application/pdf",
                ContentLength = content.Length
            });
    }

    private static AICostImportBatch BuildBatch(Guid batchId, int totalFiles, int processedFiles)
    {
        return new AICostImportBatch
        {
            Id = batchId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            CostDocumentType = CostDocumentType.ProjectCost,
            Status = AICostImportBatchStatus.Processing,
            TotalFiles = totalFiles,
            ProcessedFiles = processedFiles,
            PendingCount = 0,
            ErrorCount = 0,
            DuplicateCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static AICostImportItem BuildItem(
        Guid batchId,
        Guid itemId,
        AICostImportItemStatus status)
    {
        return new AICostImportItem
        {
            Id = itemId,
            BatchId = batchId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Status = status,
            OriginalFileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            FileHashSha256 = "abc123",
            BlobPath = "pending/path.pdf",
            RetryCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static DequeuedMessage BuildDequeuedMessage(Guid batchId, Guid itemId)
    {
        AICostImportQueueMessage payload = new AICostImportQueueMessage
        {
            BatchId = batchId,
            ItemId = itemId
        };

        return new DequeuedMessage
        {
            MessageId = "msg-1",
            PopReceipt = "pop-1",
            Text = JsonSerializer.Serialize(payload)
        };
    }

    private static async Task InvokeProcessMessageAsync(
        AICostImportWorker worker,
        DequeuedMessage message)
    {
        MethodInfo method = typeof(AICostImportWorker).GetMethod(
            "ProcessMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Task task = (Task)method.Invoke(
            worker,
            [message, "ai-cost-import-process", CancellationToken.None])!;

        await task;
    }
}
