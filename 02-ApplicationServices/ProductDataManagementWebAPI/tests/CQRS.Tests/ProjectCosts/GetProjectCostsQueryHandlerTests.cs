using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.GetProjectCosts;
using Entities.Models.Costs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.ProjectCosts;

public sealed class GetProjectCostsQueryHandlerTests
{
    private readonly Mock<IReadRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IReadRepository<SharedProjectCost>> _sharedProjectCostRepoMock = new();
    private readonly Mock<IReadRepository<BaseCostAttachment>> _attachmentRepoMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetProjectCostsQueryHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetProjectCostsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _attachmentRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync(new List<BaseCostAttachment>());

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(TenantId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMemberUserInfo>());

        _contractorServiceMock
            .Setup(s => s.GetNamesByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        _handler = new GetProjectCostsQueryHandler(
            _projectCostRepoMock.Object,
            _sharedProjectCostRepoMock.Object,
            _attachmentRepoMock.Object,
            _userServiceMock.Object,
            _blobStorageServiceMock.Object,
            _contractorServiceMock.Object,
            _currentUserMock.Object);
    }

    private static GetProjectCostsQuery BuildQuery(ResourceScope scope)
    {
        return new GetProjectCostsQuery
        {
            TenantId = TenantId,
            ProjectId = ProjectId,
            Scope = scope
        };
    }

    [Fact]
    public async Task Handle_WhenScopeAll_ReturnsProjectCosts()
    {
        // Arrange
        List<ProjectCost> costs = new List<ProjectCost>
        {
            new ProjectCost
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                ProjectId = ProjectId,
                UserId = UserId,
                Name = "Cost 1",
                SharedWith = new List<SharedProjectCost>()
            }
        };

        GetProjectCostsQuery query = BuildQuery(ResourceScope.All);

        _projectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(costs);

        // Act
        IEnumerable<ProjectCostListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenScopeMine_ReturnsOnlyUserCosts()
    {
        // Arrange
        GetProjectCostsQuery query = BuildQuery(ResourceScope.Mine);

        _projectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectCost>, IIncludableQueryable<ProjectCost, object>>[]>()))
            .ReturnsAsync(new List<ProjectCost>());

        // Act
        IEnumerable<ProjectCostListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenScopeShared_ReturnsSharedCosts()
    {
        // Arrange
        GetProjectCostsQuery query = BuildQuery(ResourceScope.Shared);

        _sharedProjectCostRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<SharedProjectCost, bool>>>(),
                It.IsAny<Func<IQueryable<SharedProjectCost>, IIncludableQueryable<SharedProjectCost, object>>[]>()))
            .ReturnsAsync(new List<SharedProjectCost>());

        // Act
        IEnumerable<ProjectCostListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenInvalidScope_ThrowsValidationApiException()
    {
        // Arrange
        GetProjectCostsQuery query = BuildQuery((ResourceScope)99);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }
}
