using Business.Implementation.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class WorkScheduleSyncServiceTests
{
    private readonly Mock<IRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkDependency>> _depRepoMock = new();
    private readonly Mock<IRepository<Entities.Models.CostTrackers.TrackedCost>> _trackedCostRepoMock = new();
    private readonly WorkScheduleSyncService _sut;

    public WorkScheduleSyncServiceTests()
    {
        _sut = new WorkScheduleSyncService(
            _groupRepoMock.Object,
            _itemRepoMock.Object,
            _stageRepoMock.Object,
            _workRepoMock.Object,
            _depRepoMock.Object,
            _trackedCostRepoMock.Object,
            NullLogger<WorkScheduleSyncService>.Instance);
    }

    // ─── SyncFromCostEstimateAsync ────────────────────────────────────────────

    [Fact]
    public async Task SyncFromCostEstimateAsync_NoCostEstimateId_ThrowsInvalidOperationException()
    {
        // Arrange
        WorkSchedule schedule = new() { CostEstimateId = null };

        // Act
        Func<Task> act = () => _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not linked to a cost estimate*");
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_NoGroups_ReturnsEmptyStagesList()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_OneRootGroup_CreatesOneStage()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].CostEstimateGroupId.Should().Be(groupId);
        _stageRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStage>()), Times.Once);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_ExistingStageForGroup_UpdatesInsteadOfInserting()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Updated Name",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        WorkScheduleStage existingStage = new()
        {
            Id = Guid.NewGuid(),
            WorkScheduleId = scheduleId,
            CostEstimateGroupId = groupId,
            Name = "Old Name",
            IsDeleted = false
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([existingStage]);

        _stageRepoMock.Setup(r => r.Update(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _stageRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStage>()), Times.Never);
        _stageRepoMock.Verify(r => r.Update(existingStage), Times.Once);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_ObsoleteStage_SoftDeletesIt()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        Guid obsoleteGroupId = Guid.NewGuid(); // group no longer exists in CE

        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        // No groups in CE
        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([]);

        // But there's an existing stage linked to a group that no longer exists
        WorkScheduleStage obsoleteStage = new()
        {
            Id = Guid.NewGuid(),
            WorkScheduleId = scheduleId,
            CostEstimateGroupId = obsoleteGroupId,
            Name = "Obsolete",
            IsDeleted = false
        };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([obsoleteStage]);

        _stageRepoMock.Setup(r => r.Update(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Works in the obsolete stage (none, simplifying test)
        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([]);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert — stage should be soft-deleted
        obsoleteStage.IsDeleted.Should().BeTrue();
        obsoleteStage.DeletedAt.Should().NotBeNull();
        _stageRepoMock.Verify(r => r.Update(obsoleteStage), Times.Once);

        // No new stages created
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_TwoRootGroups_CreatesTwoStagesInOrder()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid g1Id = Guid.NewGuid();
        Guid g2Id = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        List<CostEstimateGroup> groups =
        [
            new() { Id = g1Id, CostEstimateId = ceId, Level = 0, Order = 0, FieldValues = [] },
            new() { Id = g2Id, CostEstimateId = ceId, Level = 0, Order = 1, FieldValues = [] },
        ];

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync(groups);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        _stageRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStage>()), Times.Exactly(2));
    }

    // ─── RelationType filtering ────────────────────────────────────────────

    [Fact]
    public async Task SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeNone_CreatesWork()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        CostEstimateItem item = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.None,
            Order = 0,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = true
                }
            }
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([item]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _trackedCostRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TrackedCost, bool>>>(),
                It.IsAny<System.Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TrackedCost>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _workRepoMock.Verify(r => r.Insert(It.Is<WorkScheduleStageWork>(w => w.CostEstimateItemId == item.Id)), Times.Once);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeOption_SkipsWork()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        CostEstimateItem item = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.Option,
            Order = 0,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = true
                }
            }
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([item]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeComponent_SkipsWork()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        CostEstimateItem item = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.Component,
            Order = 0,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = true
                }
            }
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([item]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_NonWorkScopeItemWithRelationTypeNone_SkipsWork()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        CostEstimateItem item = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.None,
            Order = 0,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = false
                }
            }
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([item]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_ExistingWorkForNonMainItem_SoftDeletedOnResync()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        WorkScheduleStage existingStage = new()
        {
            Id = stageId,
            WorkScheduleId = scheduleId,
            CostEstimateGroupId = groupId,
            Name = "Stage 1",
            IsDeleted = false
        };

        CostEstimateItem item = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.Option,
            Order = 0,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = true
                }
            }
        };

        WorkScheduleStageWork existingWork = new()
        {
            Id = Guid.NewGuid(),
            WorkScheduleStageId = stageId,
            CostEstimateItemId = item.Id,
            Name = "Old Work",
            IsDeleted = false,
            TenantId = schedule.TenantId,
            ProjectId = schedule.ProjectId
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([existingStage]);

        _stageRepoMock.Setup(r => r.Update(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([item]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([existingWork]);

        _workRepoMock.Setup(r => r.UpdateRange(It.IsAny<IEnumerable<WorkScheduleStageWork>>())).Returns(Task.CompletedTask);
        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _depRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWorkDependency, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkDependency>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWorkDependency, object>>[]>()))
            .ReturnsAsync([]);

        _trackedCostRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TrackedCost, bool>>>(),
                It.IsAny<System.Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TrackedCost>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        existingWork.IsDeleted.Should().BeTrue();
        existingWork.DeletedAt.Should().NotBeNull();
        _workRepoMock.Verify(r => r.UpdateRange(It.Is<List<WorkScheduleStageWork>>(list => list.Any(w => w.IsDeleted))), Times.Once);
    }

    [Fact]
    public async Task SyncFromCostEstimateAsync_OnlyNonMainItems_NoWorksCreated()
    {
        // Arrange
        Guid ceId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid scheduleId = Guid.NewGuid();
        WorkSchedule schedule = new()
        {
            Id = scheduleId,
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = ceId
        };

        CostEstimateGroup group = new()
        {
            Id = groupId,
            CostEstimateId = ceId,
            Name = "Etap 1",
            Level = 0,
            Order = 0,
            FieldValues = new List<CostEstimateGroupFieldValue>()
        };

        CostEstimateItem optionItem = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.Option,
            Order = 0,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = true
                }
            }
        };

        CostEstimateItem componentItem = new()
        {
            Id = Guid.NewGuid(),
            CostEstimateId = ceId,
            GroupId = groupId,
            RelationType = ItemRelationType.Component,
            Order = 1,
            IsDeleted = false,
            FieldValues = new List<CostEstimateItemFieldValue>
            {
                new()
                {
                    FieldDefinition = new CostEstimateTemplateItemSystemFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        FieldType = FieldType.ItemSystemIsWorkScope
                    },
                    BoolValue = true
                }
            }
        };

        _groupRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateGroup>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateGroup, object>>[]>()))
            .ReturnsAsync([group]);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync([]);

        _stageRepoMock.Setup(r => r.Insert(It.IsAny<WorkScheduleStage>())).Returns(Task.CompletedTask);
        _stageRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync([optionItem, componentItem]);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync([]);

        _workRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never);
    }
}
