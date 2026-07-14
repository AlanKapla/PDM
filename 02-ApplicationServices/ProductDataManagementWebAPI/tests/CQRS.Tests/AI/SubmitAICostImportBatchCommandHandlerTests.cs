using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
using CQRS.AI.SubmitAICostImportBatch;
using Entities.Models.AI;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.AI;

public sealed class SubmitAICostImportBatchCommandHandlerTests
{
    private readonly Mock<IRepository<AICostImportBatch>> _batchRepoMock = new();
    private readonly Mock<IRepository<AICostImportItem>> _itemRepoMock = new();
    private readonly Mock<IAICostImportBlobService> _blobServiceMock = new();
    private readonly Mock<IQueueStorageService> _queueStorageMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<SubmitAICostImportBatchCommandHandler>> _loggerMock = new();
    private readonly SubmitAICostImportBatchCommandHandler _handler;

    public SubmitAICostImportBatchCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(AICostImportTestHelpers.UserId);

        _blobServiceMock
            .Setup(b => b.UploadPendingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("blob/path.jpg");

        _handler = new SubmitAICostImportBatchCommandHandler(
            _batchRepoMock.Object,
            _itemRepoMock.Object,
            _blobServiceMock.Object,
            _queueStorageMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTwoFiles_SubmitsBatchAndEnqueuesMessages()
    {
        // Arrange
        Mock<IFormFile> file1 = AICostImportTestHelpers.BuildFormFileMock("a.jpg");
        Mock<IFormFile> file2 = AICostImportTestHelpers.BuildFormFileMock("b.jpg");

        SubmitAICostImportBatchCommand command = new SubmitAICostImportBatchCommand
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            Files = new FormFileCollection { file1.Object, file2.Object },
            CostDocumentType = CostDocumentType.ProjectCost
        };

        // Act
        AICostImportSubmitResultWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalFiles.Should().Be(2);
        result.BatchId.Should().NotBe(Guid.Empty);

        _batchRepoMock.Verify(r => r.Insert(It.IsAny<AICostImportBatch>()), Times.Once);
        _itemRepoMock.Verify(r => r.Insert(It.IsAny<AICostImportItem>()), Times.Exactly(2));
        _queueStorageMock.Verify(
            q => q.EnqueueAsync(
                QueueNames.AICostImportProcess,
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
