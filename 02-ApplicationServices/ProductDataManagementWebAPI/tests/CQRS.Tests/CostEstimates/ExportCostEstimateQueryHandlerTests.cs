using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.ExportCostEstimate;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class ExportCostEstimateQueryHandlerTests
{
    private readonly Mock<ICostEstimateCacheService> _cacheMock = new();
    private readonly Mock<ICostEstimateAccessService> _accessMock = new();
    private readonly Mock<ICostEstimateExportService> _exportMock = new();
    private readonly Mock<IReadRepository<ProjectCurrency>> _currencyRepoMock = new();
    private readonly Mock<IReadRepository<CostEstimateFieldSchema>> _fieldSchemaRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ExportCostEstimateQueryHandler _handler;

    public ExportCostEstimateQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _handler = new ExportCostEstimateQueryHandler(
            _cacheMock.Object,
            _accessMock.Object,
            _exportMock.Object,
            _currencyRepoMock.Object,
            _fieldSchemaRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateMissing_ThrowsNotFoundApiException()
    {
        // Arrange
        ExportCostEstimateQuery query = ValidQuery();
        _cacheMock
            .Setup(s => s.GetCostEstimateAsync(
                query.CostEstimateId, query.TenantId, query.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelNone_ThrowsForbiddenApiException()
    {
        // Arrange
        ExportCostEstimateQuery query = ValidQuery();
        CostEstimate estimate = BuildEstimate(query);
        SetupCostEstimate(estimate);
        _accessMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                query.TenantId,
                query.ProjectId,
                query.CostEstimateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.None);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Theory]
    [InlineData(CostEstimateExportFormat.Pdf)]
    [InlineData(CostEstimateExportFormat.Xlsx)]
    public async Task Handle_WhenAccessAllowed_ReturnsExportFile(CostEstimateExportFormat format)
    {
        // Arrange
        ExportCostEstimateQuery query = ValidQuery() with { Format = format };
        CostEstimate estimate = BuildEstimate(query);
        SetupCostEstimate(estimate);
        SetupAccess(CostEstimateAccessLevel.ReadOnly);
        SetupEmptyDictionaries(estimate);
        SetupCurrencyAndFields(estimate.ProjectId, estimate.Id);

        CostEstimateExportFile expected = new(
            Content: [1, 2, 3],
            ContentType: format == CostEstimateExportFormat.Pdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName: $"file.{(format == CostEstimateExportFormat.Pdf ? "pdf" : "xlsx")}");

        _exportMock
            .Setup(s => s.Export(
                estimate,
                It.IsAny<IReadOnlyList<CostEstimateGroup>>(),
                It.IsAny<IReadOnlyList<CostEstimateItem>>(),
                It.IsAny<IReadOnlyList<CostEstimateAdditionalFieldWeb>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                format,
                null))
            .Returns(expected);

        // Act
        CostEstimateExportFile result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _exportMock.Verify(s => s.Export(
            estimate,
            It.IsAny<IReadOnlyList<CostEstimateGroup>>(),
            It.IsAny<IReadOnlyList<CostEstimateItem>>(),
            It.IsAny<IReadOnlyList<CostEstimateAdditionalFieldWeb>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            format,
            null), Times.Once);
    }

    private void SetupCostEstimate(CostEstimate estimate)
    {
        _cacheMock
            .Setup(s => s.GetCostEstimateAsync(
                estimate.Id, estimate.TenantId, estimate.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estimate);
    }

    private void SetupAccess(CostEstimateAccessLevel level)
    {
        _accessMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(level);
    }

    private void SetupEmptyDictionaries(CostEstimate estimate)
    {
        _cacheMock
            .Setup(s => s.GetGroupsDictionaryAsync(
                estimate.Id, estimate.TenantId, estimate.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateGroup>());

        _cacheMock
            .Setup(s => s.GetItemsDictionaryAsync(
                estimate.Id, estimate.TenantId, estimate.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, CostEstimateItem>());
    }

    private void SetupCurrencyAndFields(Guid projectId, Guid costEstimateId)
    {
        _currencyRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCurrency, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCurrency?)null);

        _fieldSchemaRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<CostEstimateFieldSchema, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<CostEstimateFieldSchema>());
    }

    private static ExportCostEstimateQuery ValidQuery() =>
        new ExportCostEstimateQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid(),
            Format = CostEstimateExportFormat.Xlsx
        };

    private static CostEstimate BuildEstimate(ExportCostEstimateQuery query) =>
        new CostEstimate
        {
            Id = query.CostEstimateId,
            TenantId = query.TenantId,
            ProjectId = query.ProjectId,
            Name = "CE",
            TotalNet = 10m,
            TotalVat = 2m,
            TotalGross = 12m
        };
}
