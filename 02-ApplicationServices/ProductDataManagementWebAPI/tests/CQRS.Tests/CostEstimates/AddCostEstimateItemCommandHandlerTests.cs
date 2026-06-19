using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.AddCostEstimateItem;
using Entities.Models.CostEstimates;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class AddCostEstimateItemCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateRecalculationService> _recalculationServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AddCostEstimateItemCommandHandler _handler;

    public AddCostEstimateItemCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new AddCostEstimateItemCommandHandler(
            _itemRepoMock.Object,
            _cacheServiceMock.Object,
            _recalculationServiceMock.Object,
            _ceAccessServiceMock.Object,
            _currentUserMock.Object);
    }

    private static CostEstimate BuildCostEstimate() =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
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

    private void SetupAccessAndGroups(CostEstimate costEstimate, CostEstimateGroup group)
    {
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
    }

    [Fact]
    public async Task Handle_WhenAddingComponentToMainPosition_ClearsParentFinancialInputFields()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateGroup group = BuildGroup(costEstimate.Id);
        Guid parentItemId = Guid.NewGuid();

        CostEstimateItem parentItem = new CostEstimateItem
        {
            Id = parentItemId,
            CostEstimateId = costEstimate.Id,
            GroupId = group.Id,
            Name = "Position",
            RelationType = ItemRelationType.None,
            Quantity = 10m,
            Unit = "szt",
            UnitPriceNet = 100m,
            UnitPriceGross = 123m,
            VatRate = 0.23m,
            NetValue = 1000m,
            GrossValue = 1230m,
            VatValue = 230m,
            IsDeleted = false
        };

        AddCostEstimateItemCommand command = new AddCostEstimateItemCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            GroupId = group.Id,
            ParentItemId = parentItemId,
            RelationType = ItemRelationType.Component,
            Order = 0
        };

        SetupAccessAndGroups(costEstimate, group);

        Dictionary<Guid, CostEstimateItem> itemsDict = new Dictionary<Guid, CostEstimateItem>
        {
            [parentItemId] = parentItem
        };

        _cacheServiceMock
            .Setup(s => s.GetItemsDictionaryAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemsDict);

        CostEstimateItem? updatedParent = null;
        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>> predicate, Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[] includes) =>
            {
                Func<CostEstimateItem, bool> compiled = predicate.Compile();
                if (compiled(parentItem))
                {
                    updatedParent = parentItem;
                    return parentItem;
                }

                return null;
            });

        _itemRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _itemRepoMock.Verify(r => r.Insert(It.IsAny<CostEstimateItem>()), Times.Once);
        _itemRepoMock.Verify(
            r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()),
            Times.Once);
        _itemRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        updatedParent.Should().NotBeNull();
        updatedParent!.Quantity.Should().BeNull();
        updatedParent.Unit.Should().BeNull();
        updatedParent.UnitPriceNet.Should().BeNull();
        updatedParent.UnitPriceGross.Should().BeNull();
        updatedParent.VatRate.Should().BeNull();
        updatedParent.NetValue.Should().Be(1000m);
        updatedParent.GrossValue.Should().Be(1230m);
        updatedParent.VatValue.Should().Be(230m);
        _recalculationServiceMock.Verify(
            s => s.RecalculateAsync(
                costEstimate.TenantId,
                costEstimate.ProjectId,
                costEstimate.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
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
}
