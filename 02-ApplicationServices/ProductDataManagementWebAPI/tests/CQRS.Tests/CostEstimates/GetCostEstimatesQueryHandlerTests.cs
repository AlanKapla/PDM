using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.GetCostEstimates;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class GetCostEstimatesQueryHandlerTests
{
    private readonly Mock<IReadRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IReadRepository<SharedCostEstimate>> _sharedCeRepoMock = new();
    private readonly Mock<ICostEstimateCacheService> _ceCacheServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IReadRepository<ProjectCurrency>> _projectCurrencyRepoMock = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly GetCostEstimatesQueryHandler _handler;

    public GetCostEstimatesQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMemberUserInfo>());

        _sharedCeRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<SharedCostEstimate, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<SharedCostEstimate>());

        _sharedCeRepoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<Expression<Func<SharedCostEstimate, bool>>>(),
                It.IsAny<Expression<Func<SharedCostEstimate, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());

        _projectCurrencyRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCurrency, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCurrency?)null);

        _handler = new GetCostEstimatesQueryHandler(
            _costEstimateRepoMock.Object,
            _sharedCeRepoMock.Object,
            _ceCacheServiceMock.Object,
            _userServiceMock.Object,
            _currentUserMock.Object,
            _projectCurrencyRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private CostEstimate BuildCostEstimate(Guid tenantId, Guid projectId) =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            OwnerId = _userId,
            TemplateId = Guid.NewGuid(),
            Name = "Test CE",
            Status = CostEstimateStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenScopeIsMine_ReturnsCostEstimateList()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        CostEstimate costEstimate = BuildCostEstimate(tenantId, projectId);

        _costEstimateRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(new List<CostEstimate> { costEstimate });

        _ceCacheServiceMock
            .Setup(s => s.GetTemplateAsync(costEstimate.TemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entities.Models.CostEstimateTemplates.CostEstimateTemplate?)null);

        GetCostEstimatesQuery query = new GetCostEstimatesQuery
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Scope = ResourceScope.Mine
        };

        // Act
        List<CostEstimateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(costEstimate.Id);
        result[0].Name.Should().Be(costEstimate.Name);
    }

    [Fact]
    public async Task Handle_WhenNoCostEstimatesFound_ReturnsEmptyList()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimate>());

        GetCostEstimatesQuery query = new GetCostEstimatesQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Scope = ResourceScope.Mine
        };

        // Act
        List<CostEstimateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenScopeIsAll_ReturnsAllCostEstimates()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        CostEstimate ce1 = BuildCostEstimate(tenantId, projectId);
        CostEstimate ce2 = BuildCostEstimate(tenantId, projectId);

        _costEstimateRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(new List<CostEstimate> { ce1, ce2 });

        _ceCacheServiceMock
            .Setup(s => s.GetTemplateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entities.Models.CostEstimateTemplates.CostEstimateTemplate?)null);

        GetCostEstimatesQuery query = new GetCostEstimatesQuery
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Scope = ResourceScope.All
        };

        // Act
        List<CostEstimateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }
}
