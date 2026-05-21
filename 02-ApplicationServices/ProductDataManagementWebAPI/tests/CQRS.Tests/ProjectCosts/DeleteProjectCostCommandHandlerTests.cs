using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.ProjectCosts.DeleteProjectCost;
using Entities.Models.Costs;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class DeleteProjectCostCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IProjectCostAccessService> _accessServiceMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IRepository<BaseCostAttachment>> _attachmentRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<DeleteProjectCostCommandHandler>> _loggerMock = new();
    private readonly Mock<ILogger<CQRS.ProjectCosts.Shared.ProjectCostHandlerBase>> _baseLoggerMock = new();
    private readonly DeleteProjectCostCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public DeleteProjectCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _attachmentRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync(new List<BaseCostAttachment>());

        _handler = new DeleteProjectCostCommandHandler(
            _projectCostRepoMock.Object,
            _accessServiceMock.Object,
            _blobStorageServiceMock.Object,
            _attachmentRepoMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object,
            _baseLoggerMock.Object);
    }

    private static ProjectCost BuildProjectCost(Guid id)
    {
        return new ProjectCost
        {
            Id = id,
            TenantId = TenantId,
            ProjectId = ProjectId,
            UserId = UserId,
            Name = "Test Cost",
            IsDeleted = false
        };
    }

    [Fact]
    public async Task Handle_WhenCostExistsAndUserHasAccess_SoftDeletesCost()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId);

        DeleteProjectCostCommand command = new DeleteProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId
        };

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(projectCost);

        _accessServiceMock
            .Setup(s => s.HasWriteAccessAsync(projectCost, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        projectCost.IsDeleted.Should().BeTrue();
        _projectCostRepoMock.Verify(r => r.Update(projectCost), Times.Once);
        _projectCostRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DeleteProjectCostCommand command = new DeleteProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = Guid.NewGuid()
        };

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
        ProjectCost projectCost = BuildProjectCost(costId);

        DeleteProjectCostCommand command = new DeleteProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId
        };

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
}
