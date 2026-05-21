using System.Linq.Expressions;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.GetCostLinkOptions;
using Entities.Models.CostEstimates;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostTrackers;

public sealed class GetCostLinkOptionsQueryHandlerTests
{
    private readonly Mock<IReadRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IReadRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<IReadRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IReadRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IReadRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IReadRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly GetCostLinkOptionsQueryHandler _handler;

    public GetCostLinkOptionsQueryHandlerTests()
    {
        _handler = new GetCostLinkOptionsQueryHandler(
            _costEstimateRepoMock.Object,
            _groupRepoMock.Object,
            _itemRepoMock.Object,
            _workScheduleRepoMock.Object,
            _stageRepoMock.Object,
            _stageWorkRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetCostLinkOptionsQuery BuildQuery(Guid tenantId, Guid projectId) =>
        new GetCostLinkOptionsQuery
        {
            TenantId = tenantId,
            ProjectId = projectId
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoEstimatesAndNoSchedules_ReturnsEmptyCollections()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _costEstimateRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(new List<CostEstimate>());

        _workScheduleRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(new List<WorkSchedule>());

        GetCostLinkOptionsQuery query = BuildQuery(tenantId, projectId);

        // Act
        CostLinkOptionsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EstimateItems.Should().BeEmpty();
        result.WorkItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenEstimatesWithItemsExist_ReturnsEstimateItems()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid estimateId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        CostEstimate estimate = new CostEstimate
        {
            Id = estimateId,
            TenantId = tenantId,
            ProjectId = projectId,
            Name = "Estimate A"
        };

        CostEstimateGroup group = new CostEstimateGroup
        {
            Id = groupId,
            CostEstimateId = estimateId,
            Name = "Group A",
            ParentGroupId = null
        };

        CostEstimateItem item = new CostEstimateItem
        {
            Id = Guid.NewGuid(),
            CostEstimateId = estimateId,
            GroupId = groupId,
            Name = "Item A",
            RelationType = ItemRelationType.None
        };

        _costEstimateRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(new List<CostEstimate> { estimate });

        _groupRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateGroup, bool>>>()))
            .ReturnsAsync(new List<CostEstimateGroup> { group });

        _itemRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateItem, bool>>>()))
            .ReturnsAsync(new List<CostEstimateItem> { item });

        _workScheduleRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(new List<WorkSchedule>());

        GetCostLinkOptionsQuery query = BuildQuery(tenantId, projectId);

        // Act
        CostLinkOptionsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EstimateItems.Should().HaveCount(1);
        result.EstimateItems[0].ItemId.Should().Be(item.Id);
        result.WorkItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSchedulesWithStagesAndWorksExist_ReturnsWorkItems()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();

        WorkSchedule schedule = new WorkSchedule
        {
            Id = scheduleId,
            TenantId = tenantId,
            ProjectId = projectId,
            Name = "Schedule A"
        };

        WorkScheduleStage stage = new WorkScheduleStage
        {
            Id = stageId,
            TenantId = tenantId,
            ProjectId = projectId,
            WorkScheduleId = scheduleId,
            Name = "Stage A",
            ParentStageId = null
        };

        WorkScheduleStageWork work = new WorkScheduleStageWork
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            WorkScheduleStageId = stageId,
            Name = "Work A",
            ColorRgb = "#FF0000"
        };

        _costEstimateRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(new List<CostEstimate>());

        _workScheduleRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(new List<WorkSchedule> { schedule });

        _stageRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkScheduleStage, bool>>>()))
            .ReturnsAsync(new List<WorkScheduleStage> { stage });

        _stageWorkRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>()))
            .ReturnsAsync(new List<WorkScheduleStageWork> { work });

        GetCostLinkOptionsQuery query = BuildQuery(tenantId, projectId);

        // Act
        CostLinkOptionsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EstimateItems.Should().BeEmpty();
        result.WorkItems.Should().HaveCount(1);
        result.WorkItems[0].WorkId.Should().Be(work.Id);
    }
}
