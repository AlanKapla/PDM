using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimates.GetCostEstimateDetails;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Entities.Models.Projects;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class GetCostEstimateDetailsQueryHandlerTests
{
    private readonly Mock<ICostEstimateCacheService> _ceCacheServiceMock = new();
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<IReadRepository<SharedCostEstimate>> _sharedCeRepoMock = new();
    private readonly Mock<IReadRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IReadRepository<ProjectCurrency>> _projectCurrencyRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetCostEstimateDetailsQueryHandler _handler;

    public GetCostEstimateDetailsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        _projectCurrencyRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCurrency, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCurrency?)null);

        _ceCacheServiceMock
            .Setup(s => s.GetGroupsDictionaryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateGroup>());

        _ceCacheServiceMock
            .Setup(s => s.GetGroupFieldValuesDictionaryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateGroupFieldValue>());

        _ceCacheServiceMock
            .Setup(s => s.GetItemsDictionaryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateItem>());

        _ceCacheServiceMock
            .Setup(s => s.GetItemFieldValuesDictionaryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateItemFieldValue>());

        _templateServiceMock
            .Setup(s => s.GetTemplateStructureCachedAsync(
                It.IsAny<CostEstimateTemplate>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CostEstimateTemplateStructureWeb(
                TemplateId: Guid.NewGuid(),
                MaxGroupLevel: null,
                Units: new List<UnitWeb>(),
                Categories: new List<CategoryWeb>(),
                GroupHeaderFields: new List<FieldDefinitionWeb>(),
                SystemFields: new List<FieldDefinitionWeb>(),
                CalculatedFields: new List<FieldDefinitionWeb>(),
                GenericFields: new List<FieldDefinitionWeb>(),
                UiConfiguration: null));

        _handler = new GetCostEstimateDetailsQueryHandler(
            _ceCacheServiceMock.Object,
            _templateServiceMock.Object,
            _blobStorageServiceMock.Object,
            _cacheServiceMock.Object,
            _ceAccessServiceMock.Object,
            _sharedCeRepoMock.Object,
            _workScheduleRepoMock.Object,
            _projectCurrencyRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimate BuildCostEstimate(Guid? tenantId = null, Guid? projectId = null) =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test CE",
            Status = CostEstimateStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Owner = new Entities.Models.Users.User { FirstName = "Test", LastName = "Owner" }
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCostEstimateExistsWithReadOnlyAccess_ReturnsDetails()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        CostEstimateTemplate template = new CostEstimateTemplate
        {
            Id = costEstimate.TemplateId,
            Name = "Test Template",
            OwnerId = Guid.NewGuid(),
            IsDeleted = false
        };

        _ceCacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                costEstimate.Id,
                costEstimate.TenantId,
                costEstimate.ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                costEstimate.TenantId,
                costEstimate.ProjectId,
                costEstimate.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.ReadOnly);

        _ceCacheServiceMock
            .Setup(s => s.GetTemplateAsync(costEstimate.TemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        GetCostEstimateDetailsQuery query = new GetCostEstimateDetailsQuery
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id
        };

        // Act
        CostEstimateDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(costEstimate.Id);
        result.Name.Should().Be(costEstimate.Name);
        result.TemplateName.Should().Be(template.Name);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _ceCacheServiceMock
            .Setup(s => s.GetCostEstimateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        GetCostEstimateDetailsQuery query = new GetCostEstimateDetailsQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelIsNone_ThrowsForbiddenApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();

        _ceCacheServiceMock
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

        GetCostEstimateDetailsQuery query = new GetCostEstimateDetailsQuery
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
