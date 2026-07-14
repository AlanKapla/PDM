using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.AI.AcceptAICostImportItem;
using CQRS.ProjectCosts.CreateProjectCost;
using Entities.Enums;
using Entities.Models.AI;
using Entities.Models.Costs;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.AI;

public sealed class AcceptAICostImportItemCommandHandlerTests
{
    private readonly Mock<IRepository<AICostImportItem>> _itemRepoMock = new();
    private readonly Mock<IReadRepository<AICostImportBatch>> _batchRepoMock = new();
    private readonly Mock<IRepository<BaseCost>> _costRepoMock = new();
    private readonly Mock<IAICostImportBlobService> _blobServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IAccessService> _accessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<AcceptAICostImportItemCommandHandler>> _loggerMock = new();
    private readonly AcceptAICostImportItemCommandHandler _handler;

    public AcceptAICostImportItemCommandHandlerTests()
    {
        _accessServiceMock
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<string>(),
                It.IsAny<ResourceRef>(),
                It.IsAny<ResourceScope?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _blobServiceMock
            .Setup(b => b.GeneratePendingPreviewUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://preview.example.com/file.jpg");

        _handler = new AcceptAICostImportItemCommandHandler(
            _itemRepoMock.Object,
            _batchRepoMock.Object,
            _costRepoMock.Object,
            _blobServiceMock.Object,
            _mediatorMock.Object,
            _accessServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync((AICostImportItem?)null);

        AcceptAICostImportItemCommand command = BuildCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenItemIsPending_CreatesCostAndMarksAccepted()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        AICostImportItem item = AICostImportTestHelpers.BuildItem();
        AICostImportBatch batch = AICostImportTestHelpers.BuildBatch();

        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(item);

        _batchRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportBatch, bool>>>()))
            .ReturnsAsync(batch);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateProjectCostCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectCostListItemWeb
            {
                Id = costId,
                UserId = AICostImportTestHelpers.UserId,
                UserName = "Test User",
                Name = "Materiały budowlane",
                ApprovalStatus = CostApprovalStatus.Draft,
                CreatedAt = DateTime.UtcNow
            });

        BaseCost cost = new ProjectCost
        {
            Id = costId,
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            Name = "Materiały budowlane"
        };

        _costRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<BaseCost, bool>>>()))
            .ReturnsAsync(cost);

        AcceptAICostImportItemCommand command = BuildCommand();

        // Act
        AICostImportItemWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        item.Status.Should().Be(AICostImportItemStatus.Accepted);
        item.AcceptedCostId.Should().Be(costId);
        cost.SourceFileHashSha256.Should().Be(item.FileHashSha256);

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<CreateProjectCostCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _blobServiceMock.Verify(
            b => b.MoveToCostAttachmentAsync(
                It.IsAny<BaseCost>(),
                item.BlobPath,
                item.OriginalFileName,
                item.ContentType,
                item.FileSizeBytes,
                AICostImportTestHelpers.TenantId,
                AICostImportTestHelpers.ProjectId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AcceptAICostImportItemCommand BuildCommand() =>
        new AcceptAICostImportItemCommand
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            ItemId = AICostImportTestHelpers.ItemId
        };
}
