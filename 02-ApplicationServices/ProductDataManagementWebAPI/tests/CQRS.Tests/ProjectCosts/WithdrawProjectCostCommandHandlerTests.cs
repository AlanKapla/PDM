using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.WithdrawProjectCost;
using Entities.Models.Costs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class WithdrawProjectCostCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IProjectCostAccessService> _accessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<WithdrawProjectCostCommandHandler>> _loggerMock = new();
    private readonly WithdrawProjectCostCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public WithdrawProjectCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.FullName).Returns("Test User");

        _handler = new WithdrawProjectCostCommandHandler(
            _projectCostRepoMock.Object,
            _accessServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private static ProjectCost BuildProjectCost(Guid costId, CostApprovalStatus status = CostApprovalStatus.PendingApproval)
    {
        return new ProjectCost
        {
            Id = costId,
            TenantId = TenantId,
            ProjectId = ProjectId,
            UserId = UserId,
            Name = "Test Cost",
            IsDeleted = false,
            ApprovalStatus = status
        };
    }

    private static WithdrawProjectCostCommand BuildCommand(Guid costId)
    {
        return new WithdrawProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId
        };
    }

    [Fact]
    public async Task Handle_WhenCostExistsAndUserHasWriteAccess_WithdrawsCostAndReturnsWeb()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId, CostApprovalStatus.PendingApproval);
        WithdrawProjectCostCommand command = BuildCommand(costId);

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(projectCost);

        _accessServiceMock
            .Setup(s => s.HasWriteAccessAsync(projectCost, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        ProjectCostListItemWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(costId);
        result.ApprovalStatus.Should().Be(CostApprovalStatus.Draft);
        projectCost.ApprovalStatus.Should().Be(CostApprovalStatus.Draft);
        projectCost.ApprovedByUserId.Should().BeNull();
        projectCost.ApprovedAt.Should().BeNull();
        _projectCostRepoMock.Verify(r => r.Update(projectCost), Times.Once);
        _projectCostRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        WithdrawProjectCostCommand command = BuildCommand(Guid.NewGuid());

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync((ProjectCost?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoWriteAccess_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId, CostApprovalStatus.PendingApproval);
        WithdrawProjectCostCommand command = BuildCommand(costId);

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(projectCost);

        _accessServiceMock
            .Setup(s => s.HasWriteAccessAsync(projectCost, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenCostIsNotPendingApproval_ThrowsValidationApiException()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId, CostApprovalStatus.Draft);
        WithdrawProjectCostCommand command = BuildCommand(costId);

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(projectCost);

        _accessServiceMock
            .Setup(s => s.HasWriteAccessAsync(projectCost, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }
}
