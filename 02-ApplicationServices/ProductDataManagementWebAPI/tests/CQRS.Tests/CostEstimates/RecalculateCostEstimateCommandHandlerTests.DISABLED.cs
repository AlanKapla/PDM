using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.RecalculateCostEstimate;
using Entities.Models.CostEstimates;
// using Entities.Models.CostEstimateTemplates; // Removed
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class RecalculateCostEstimateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItemFieldValue>> _itemFieldValueRepoMock = new();
    private readonly Mock<ICostEstimateCalculationService> _calculationServiceMock = new();
    private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly RecalculateCostEstimateCommandHandler _handler;

    public RecalculateCostEstimateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new RecalculateCostEstimateCommandHandler(
            _costEstimateRepoMock.Object,
            _groupRepoMock.Object,
            _itemRepoMock.Object,
            _itemFieldValueRepoMock.Object,
            _calculationServiceMock.Object,
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

    private static RecalculateCostEstimateCommand ValidCommand(CostEstimate costEstimate) =>
        new RecalculateCostEstimateCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFullAccess_RecalculatesAndSaves()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = costEstimate.TemplateId,
            OwnerId = Guid.NewGuid(),
            Name = "Template",
            IsDeleted = false
        };

        RecalculateCostEstimateCommand command = ValidCommand(costEstimate);

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
            .Setup(s => s.GetTemplateAsync(costEstimate.TemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _groupRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateGroup, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimateGroup>());

        _itemRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimateItem, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimateItem>());

        _itemFieldValueRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<CostEstimateItemFieldValue, bool>>>(),
                It.IsAny<Func<IQueryable<CostEstimateItemFieldValue>, IIncludableQueryable<CostEstimateItemFieldValue, object>>[]>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimateItemFieldValue>());

        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(costEstimate);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _calculationServiceMock.Verify(s => s.RecalculateCostEstimate(It.IsAny<CostEstimate>()), Times.Once);
        _costEstimateRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(s => s.InvalidateCostEstimateAsync(
            costEstimate.Id,
            costEstimate.TenantId,
            costEstimate.ProjectId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateNotFoundInCache_ThrowsNotFoundApiException()
    {
        // Arrange
        _cacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        RecalculateCostEstimateCommand command = new RecalculateCostEstimateCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid()
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
        RecalculateCostEstimateCommand command = ValidCommand(costEstimate);

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

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelIsReadOnly_ThrowsForbiddenApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        RecalculateCostEstimateCommand command = ValidCommand(costEstimate);

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
            .ReturnsAsync(CostEstimateAccessLevel.ReadOnly);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}

