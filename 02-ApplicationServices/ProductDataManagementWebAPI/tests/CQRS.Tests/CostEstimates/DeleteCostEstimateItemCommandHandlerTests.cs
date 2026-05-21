using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.DeleteCostEstimateItem;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class DeleteCostEstimateItemCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItemFieldValue>> _itemFieldValueRepoMock = new();
    private readonly Mock<IRepository<CostEstimateFieldFile>> _fieldFileRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<DeleteCostEstimateItemCommandHandler>> _loggerMock = new();
    private readonly DeleteCostEstimateItemCommandHandler _handler;

    public DeleteCostEstimateItemCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new DeleteCostEstimateItemCommandHandler(
            _itemRepoMock.Object,
            _itemFieldValueRepoMock.Object,
            _fieldFileRepoMock.Object,
            _stageWorkRepoMock.Object,
            _trackedCostRepoMock.Object,
            _cacheServiceMock.Object,
            _ceAccessServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimate BuildCostEstimate() =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            Name = "Test CE",
            Status = CostEstimateStatus.Draft,
            IsDeleted = false
        };

    private static CostEstimateItem BuildItem(Guid costEstimateId, Guid groupId) =>
        new CostEstimateItem
        {
            Id = Guid.NewGuid(),
            CostEstimateId = costEstimateId,
            GroupId = groupId,
            Name = "Item 1",
            RelationType = ItemRelationType.None,
            Order = 0,
            IsDeleted = false
        };

    private void SetupHappyPath(CostEstimate costEstimate, Guid itemId, Dictionary<Guid, CostEstimateItem> itemsDict)
    {
        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        _cacheServiceMock
            .Setup(s => s.GetItemsDictionaryAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemsDict);

        _fieldFileRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateFieldFile, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimateFieldFile>());

        _itemRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateItem, bool>>>()))
            .ReturnsAsync(itemsDict.Values.Where(i => i.Id == itemId));
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidRequest_DeletesItemAndInvalidatesCache()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateItem item = BuildItem(costEstimate.Id, Guid.NewGuid());
        Dictionary<Guid, CostEstimateItem> itemsDict = new Dictionary<Guid, CostEstimateItem>
        {
            [item.Id] = item
        };

        SetupHappyPath(costEstimate, item.Id, itemsDict);

        DeleteCostEstimateItemCommand command = new DeleteCostEstimateItemCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            ItemId = item.Id
        };

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _cacheServiceMock.Verify(s => s.InvalidateCostEstimateAsync(
            costEstimate.Id,
            costEstimate.TenantId,
            costEstimate.ProjectId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        DeleteCostEstimateItemCommand command = new DeleteCostEstimateItemCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid(),
            ItemId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        Guid unknownItemId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        _cacheServiceMock
            .Setup(s => s.GetItemsDictionaryAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateItem>());

        DeleteCostEstimateItemCommand command = new DeleteCostEstimateItemCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            ItemId = unknownItemId
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
