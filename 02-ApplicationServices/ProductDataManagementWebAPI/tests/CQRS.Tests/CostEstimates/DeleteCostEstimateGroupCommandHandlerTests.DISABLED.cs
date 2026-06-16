using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.DeleteCostEstimateGroup;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class DeleteCostEstimateGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<CostEstimateGroupFieldValue>> _groupFieldValueRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItemFieldValue>> _itemFieldValueRepoMock = new();
    private readonly Mock<IRepository<CostEstimateFieldFile>> _fieldFileRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<DeleteCostEstimateGroupCommandHandler>> _loggerMock = new();
    private readonly DeleteCostEstimateGroupCommandHandler _handler;

    public DeleteCostEstimateGroupCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new DeleteCostEstimateGroupCommandHandler(
            _groupRepoMock.Object,
            _itemRepoMock.Object,
            _groupFieldValueRepoMock.Object,
            _itemFieldValueRepoMock.Object,
            _fieldFileRepoMock.Object,
            _stageRepoMock.Object,
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

    private static CostEstimateGroup BuildGroup(Guid costEstimateId) =>
        new CostEstimateGroup
        {
            Id = Guid.NewGuid(),
            CostEstimateId = costEstimateId,
            Name = "Group 1",
            Level = 0,
            Order = 0,
            IsDeleted = false
        };

    private void SetupHappyPath(CostEstimate costEstimate, Guid groupId)
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

        Dictionary<Guid, CostEstimateGroup> groupsDict = new Dictionary<Guid, CostEstimateGroup>
        {
            [groupId] = new CostEstimateGroup
            {
                Id = groupId,
                CostEstimateId = costEstimate.Id,
                Name = "G1",
                Level = 0,
                Order = 0,
                IsDeleted = false
            }
        };

        _cacheServiceMock
            .Setup(s => s.GetGroupsDictionaryAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(groupsDict);

        _cacheServiceMock
            .Setup(s => s.GetItemsDictionaryAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateItem>());

        _fieldFileRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateFieldFile, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimateFieldFile>());
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidRequest_DeletesGroupAndInvalidatesCache()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        Guid groupId = Guid.NewGuid();

        SetupHappyPath(costEstimate, groupId);

        DeleteCostEstimateGroupCommand command = new DeleteCostEstimateGroupCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            GroupId = groupId
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

        DeleteCostEstimateGroupCommand command = new DeleteCostEstimateGroupCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid(),
            GroupId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelIsNone_ThrowsForbiddenApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();

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
            .ReturnsAsync(CostEstimateAccessLevel.None);

        DeleteCostEstimateGroupCommand command = new DeleteCostEstimateGroupCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            GroupId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenGroupNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        Guid unknownGroupId = Guid.NewGuid();

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
            .Setup(s => s.GetGroupsDictionaryAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateGroup>());

        DeleteCostEstimateGroupCommand command = new DeleteCostEstimateGroupCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            GroupId = unknownGroupId
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
