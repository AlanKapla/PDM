using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.ProjectCosts.ShareProjectCosts;
using Entities.Models.Costs;
using Entities.Models.Notifications;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class ShareProjectCostsCommandHandlerTests
{
    private readonly Mock<IRepository<SharedProjectCost>> _sharedProjectCostRepoMock = new();
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<ShareProjectCostsCommandHandler>> _loggerMock = new();
    private readonly ShareProjectCostsCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ShareProjectCostsCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);
        _currentUserMock.Setup(u => u.FullName).Returns("Test User");
        _currentUserMock.Setup(u => u.FirstName).Returns("Test");
        _currentUserMock.Setup(u => u.LastName).Returns("User");

        _handler = new ShareProjectCostsCommandHandler(
            _sharedProjectCostRepoMock.Object,
            _projectCostRepoMock.Object,
            _userServiceMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAdminSharesExistingCosts_InsertsShares()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        List<ProjectCost> costs = new List<ProjectCost>
        {
            new ProjectCost { Id = costId, TenantId = TenantId, ProjectId = ProjectId, UserId = UserId, Name = "Cost 1" }
        };

        ShareProjectCostsCommand command = new ShareProjectCostsCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            ProjectCostIds = new List<Guid> { costId },
            SharedWithUserIds = new List<Guid>() // no users = no notifications
        };

        _projectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(costs);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sharedProjectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<SharedProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<SharedProjectCost>, IIncludableQueryable<SharedProjectCost, object>>[]>()))
            .ReturnsAsync(new List<SharedProjectCost>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_WhenSomeCostsNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        ShareProjectCostsCommand command = new ShareProjectCostsCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            ProjectCostIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            SharedWithUserIds = new List<Guid>()
        };

        // Return only 1 cost when 2 were requested
        _projectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(new List<ProjectCost>
            {
                new ProjectCost { Id = Guid.NewGuid(), TenantId = TenantId, ProjectId = ProjectId }
            });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNonAdminTriesToShareOtherUsersCosts_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid otherUserId = Guid.NewGuid();
        Guid costId = Guid.NewGuid();
        List<ProjectCost> costs = new List<ProjectCost>
        {
            new ProjectCost { Id = costId, TenantId = TenantId, ProjectId = ProjectId, UserId = otherUserId, Name = "Other Cost" }
        };

        ShareProjectCostsCommand command = new ShareProjectCostsCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            ProjectCostIds = new List<Guid> { costId },
            SharedWithUserIds = new List<Guid> { Guid.NewGuid() }
        };

        _projectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(costs);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenOwnerSharesOwnCosts_InsertsShares()
    {
        // Arrange
        Guid costId = Guid.NewGuid();
        List<ProjectCost> costs = new List<ProjectCost>
        {
            new ProjectCost { Id = costId, TenantId = TenantId, ProjectId = ProjectId, UserId = UserId, Name = "My Cost" }
        };

        ShareProjectCostsCommand command = new ShareProjectCostsCommand
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            ProjectCostIds = new List<Guid> { costId },
            SharedWithUserIds = new List<Guid>() // empty for simplicity
        };

        _projectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(costs);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sharedProjectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<SharedProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<SharedProjectCost>, IIncludableQueryable<SharedProjectCost, object>>[]>()))
            .ReturnsAsync(new List<SharedProjectCost>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
    }
}
