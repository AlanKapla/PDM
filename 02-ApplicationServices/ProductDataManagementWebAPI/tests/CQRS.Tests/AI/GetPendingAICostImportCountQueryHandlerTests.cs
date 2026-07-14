using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.GetPendingAICostImportCount;
using Entities.Enums;
using Entities.Models.AI;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.AI;

public sealed class GetPendingAICostImportCountQueryHandlerTests
{
    private readonly Mock<IReadRepository<AICostImportItem>> _itemRepoMock = new();
    private readonly Mock<IReadRepository<AICostImportBatch>> _batchRepoMock = new();
    private readonly Mock<IAccessService> _accessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetPendingAICostImportCountQueryHandler _handler;

    public GetPendingAICostImportCountQueryHandlerTests()
    {
        _accessServiceMock
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<string>(),
                It.IsAny<ResourceRef>(),
                It.IsAny<ResourceScope?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new GetPendingAICostImportCountQueryHandler(
            _itemRepoMock.Object,
            _batchRepoMock.Object,
            _accessServiceMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMixedStatuses_ReturnsBreakdown()
    {
        // Arrange
        AICostImportItem pendingItem = AICostImportTestHelpers.BuildItem();
        AICostImportItem errorItem = AICostImportTestHelpers.BuildItem();
        errorItem.Id = Guid.NewGuid();
        errorItem.Status = AICostImportItemStatus.ErrorNeedsReview;
        AICostImportItem duplicateItem = AICostImportTestHelpers.BuildItem();
        duplicateItem.Id = Guid.NewGuid();
        duplicateItem.Status = AICostImportItemStatus.DuplicateDetected;
        AICostImportBatch batch = AICostImportTestHelpers.BuildBatch();

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(new List<AICostImportItem> { pendingItem, errorItem, duplicateItem });

        _batchRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportBatch, bool>>>()))
            .ReturnsAsync(batch);

        GetPendingAICostImportCountQuery query = new GetPendingAICostImportCountQuery
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId
        };

        // Act
        PendingAICostImportCountWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PendingCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.DuplicateCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNoItems_ReturnsZeroCounts()
    {
        // Arrange
        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(new List<AICostImportItem>());

        GetPendingAICostImportCountQuery query = new GetPendingAICostImportCountQuery
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId
        };

        // Act
        PendingAICostImportCountWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PendingCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.DuplicateCount.Should().Be(0);
    }
}
