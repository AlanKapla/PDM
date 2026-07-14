using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.ApproveProjectCost;
using Entities.Models.Costs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class ApproveProjectCostCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<ApproveProjectCostCommandHandler>> _loggerMock = new();
    private readonly ApproveProjectCostCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ApproveProjectCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.FullName).Returns("Admin User");

        _handler = new ApproveProjectCostCommandHandler(
            _projectCostRepoMock.Object,
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
            UserId = Guid.NewGuid(),
            Name = "Test Cost",
            IsDeleted = false,
            ApprovalStatus = status
        };
    }

    private static ApproveProjectCostCommand BuildCommand(Guid costId)
    {
        return new ApproveProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId
        };
    }

    [Fact]
    public async Task Handle_WhenUserIsAdminAndCostIsPendingApproval_ApprovesCostAndReturnsWeb()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId, CostApprovalStatus.PendingApproval);
        ApproveProjectCostCommand command = BuildCommand(costId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(projectCost);

        // Act
        ProjectCostListItemWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(costId);
        result.ApprovalStatus.Should().Be(CostApprovalStatus.Approved);
        projectCost.ApprovalStatus.Should().Be(CostApprovalStatus.Approved);
        projectCost.ApprovedByUserId.Should().Be(UserId);
        projectCost.ApprovedAt.Should().NotBeNull();
        _projectCostRepoMock.Verify(r => r.Update(projectCost), Times.Once);
        _projectCostRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAdmin_ThrowsForbiddenApiException()
    {
        // Arrange
        ApproveProjectCostCommand command = BuildCommand(Guid.NewGuid());

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenCostNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        ApproveProjectCostCommand command = BuildCommand(Guid.NewGuid());

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

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
    public async Task Handle_WhenCostIsNotPendingApproval_ThrowsValidationApiException()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId, CostApprovalStatus.Draft);
        ApproveProjectCostCommand command = BuildCommand(costId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(projectCost);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }
}
