using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.AddCostEstimateItem;
using Entities.Models.CostEstimates;
// using Entities.Models.CostEstimateTemplates; // Removed
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class AddCostEstimateItemCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AddCostEstimateItemCommandHandler _handler;

    public AddCostEstimateItemCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new AddCostEstimateItemCommandHandler(
            _itemRepoMock.Object,
            _cacheServiceMock.Object,
            _ceAccessServiceMock.Object,
            _currentUserMock.Object);
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

    private static AddCostEstimateItemCommand ValidCommand(CostEstimate costEstimate, Guid groupId) =>
        new AddCostEstimateItemCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            GroupId = groupId,
            ParentItemId = null,
            RelationType = ItemRelationType.None,
            Order = 0
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidRequest_InsertsItemAndReturnsGuid()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateGroup group = BuildGroup(costEstimate.Id);
        AddCostEstimateItemCommand command = ValidCommand(costEstimate, group.Id);

        Dictionary<Guid, CostEstimateGroup> groupsDict = new Dictionary<Guid, CostEstimateGroup>
        {
            [group.Id] = group
        };

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
            .ReturnsAsync(groupsDict);

        _cacheServiceMock
            .Setup(s => s.GetTemplateAsync(costEstimate.TemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CostEstimateTemplate
            {
                Id = costEstimate.TemplateId,
                OwnerId = Guid.NewGuid(),
                Name = "Template",
                IsDeleted = false
            });

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _itemRepoMock.Verify(r => r.Insert(It.IsAny<CostEstimateItem>()), Times.Once);
        _cacheServiceMock.Verify(s => s.InvalidateItemsAsync(
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

        AddCostEstimateItemCommand command = new AddCostEstimateItemCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            RelationType = ItemRelationType.None,
            Order = 0
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenGroupNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        Guid unknownGroupId = Guid.NewGuid();
        AddCostEstimateItemCommand command = ValidCommand(costEstimate, unknownGroupId);

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

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}

