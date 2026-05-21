using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.UpdateCostEstimate;
using Entities.Models.CostEstimates;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class UpdateCostEstimateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _ceCacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateCostEstimateCommandHandler _handler;

    public UpdateCostEstimateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new UpdateCostEstimateCommandHandler(
            _costEstimateRepoMock.Object,
            _ceCacheServiceMock.Object,
            _ceAccessServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimate BuildCostEstimate(Guid? tenantId = null, Guid? projectId = null) =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            Name = "Old Name",
            Status = CostEstimateStatus.Draft,
            IsDeleted = false
        };

    private static UpdateCostEstimateCommand ValidCommand(CostEstimate costEstimate) =>
        new UpdateCostEstimateCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id,
            Name = "Updated Name",
            Description = "Updated Description"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFullAccess_UpdatesAndInvalidatesCache()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        UpdateCostEstimateCommand command = ValidCommand(costEstimate);

        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _costEstimateRepoMock.Verify(r => r.Update(It.IsAny<CostEstimate>()), Times.Once);
        _ceCacheServiceMock.Verify(s => s.InvalidateCostEstimateAsync(
            command.CostEstimateId,
            command.TenantId,
            command.ProjectId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync((CostEstimate?)null);

        UpdateCostEstimateCommand command = new UpdateCostEstimateCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid(),
            Name = "Name"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelIsNotFull_ThrowsForbiddenApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        UpdateCostEstimateCommand command = ValidCommand(costEstimate);

        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Restricted);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
