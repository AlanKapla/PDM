using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.Costs;
using Entities.Models.Notifications;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;
using CQRS.ProjectCosts.UpdateCostShare;
using Business.Interfaces.Services;

namespace CQRS.Tests.ProjectCosts;

public sealed class UpdateCostShareCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IRepository<SharedProjectCost>> _sharedProjectCostRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<UpdateCostShareCommandHandler>> _loggerMock = new();
    private readonly UpdateCostShareCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public UpdateCostShareCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.FirstName).Returns("Test");
        _currentUserMock.Setup(u => u.LastName).Returns("User");

        _handler = new UpdateCostShareCommandHandler(
            _projectCostRepoMock.Object,
            _sharedProjectCostRepoMock.Object,
            _userRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private static ProjectCost BuildProjectCost(Guid costId, Guid? ownerId = null)
    {
        return new ProjectCost
        {
            Id = costId,
            TenantId = TenantId,
            ProjectId = ProjectId,
            UserId = ownerId ?? UserId,
            Name = "Test Cost",
            SharedWith = new List<SharedProjectCost>()
        };
    }

    [Fact]
    public async Task Handle_WhenCostExistsAndUserIsOwner_UpdatesShares()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        ProjectCost cost = BuildProjectCost(costId);

        UpdateCostShareCommand command = new UpdateCostShareCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId,
            SharedWithUserIds = new List<Guid>()
        };

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(cost);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(new List<User>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _sharedProjectCostRepoMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateCostShareCommand command = new UpdateCostShareCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = Guid.NewGuid(),
            SharedWithUserIds = new List<Guid>()
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
    public async Task Handle_WhenNonAdminNonOwnerTriesToUpdateShares_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        ProjectCost cost = BuildProjectCost(costId, otherUserId);

        UpdateCostShareCommand command = new UpdateCostShareCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId,
            SharedWithUserIds = new List<Guid>()
        };

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(cost);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenAdminUpdatesShares_UpdatesSuccessfully()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        ProjectCost cost = BuildProjectCost(costId, otherUserId);

        UpdateCostShareCommand command = new UpdateCostShareCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            CostId = costId,
            SharedWithUserIds = new List<Guid>()
        };

        _projectCostRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(cost);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(new List<User>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
    }
}
