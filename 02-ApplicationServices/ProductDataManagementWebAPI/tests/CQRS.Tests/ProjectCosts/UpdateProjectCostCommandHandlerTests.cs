using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.UpdateProjectCost;
using Entities.Models.Costs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class UpdateProjectCostCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IProjectCostAccessService> _accessServiceMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IRepository<BaseCostAttachment>> _attachmentRepoMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<UpdateProjectCostCommandHandler>> _loggerMock = new();
    private readonly Mock<ILogger<CQRS.ProjectCosts.Shared.ProjectCostHandlerBase>> _baseLoggerMock = new();
    private readonly UpdateProjectCostCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public UpdateProjectCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.FullName).Returns("Test User");

        _attachmentRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync(new List<BaseCostAttachment>());

        _contractorServiceMock
            .Setup(s => s.GetNamesByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        _handler = new UpdateProjectCostCommandHandler(
            _projectCostRepoMock.Object,
            _accessServiceMock.Object,
            _blobStorageServiceMock.Object,
            _attachmentRepoMock.Object,
            _contractorServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object,
            _baseLoggerMock.Object);
    }

    private static ProjectCost BuildProjectCost(Guid costId)
    {
        return new ProjectCost
        {
            Id = costId,
            TenantId = TenantId,
            ProjectId = ProjectId,
            UserId = UserId,
            Name = "Old Name"
        };
    }

    private static UpdateProjectCostCommand BuildCommand(Guid costId)
    {
        return new UpdateProjectCostCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId,
            Name = "Updated Name",
            Net = 2000m,
            RemoveDocument = false
        };
    }

    [Fact]
    public async Task Handle_WhenCostExistsAndUserHasWriteAccess_UpdatesAndReturnsWebModel()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost projectCost = BuildProjectCost(costId);
        UpdateProjectCostCommand command = BuildCommand(costId);

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
        result.Name.Should().Be(command.Name);
        _projectCostRepoMock.Verify(r => r.Update(projectCost), Times.Once);
        _projectCostRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateProjectCostCommand command = BuildCommand(Guid.NewGuid());

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
        UpdateProjectCostCommand command = BuildCommand(costId);

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
