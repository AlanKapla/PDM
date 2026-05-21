using Business.Implementation.Services;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class CostEstimateTemplateServiceTests
{
    private readonly Mock<IRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateUnit>> _unitRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateCategory>> _categoryRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateGroupFieldDefinition>> _groupFieldRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateItemSystemFieldDefinition>> _systemFieldRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateItemCalculatedFieldDefinition>> _calculatedFieldRepoMock = new();
    private readonly Mock<IRepository<CostEstimateTemplateItemGenericFieldDefinition>> _genericFieldRepoMock = new();
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<CostEstimateFieldFile>> _fieldFileRepoMock = new();
    private readonly Mock<ICostEstimateCalculationService> _calculationServiceMock = new();
    private readonly Mock<IBlobStorageService> _blobMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly CostEstimateTemplateService _sut;

    public CostEstimateTemplateServiceTests()
    {
        _sut = new CostEstimateTemplateService(
            _templateRepoMock.Object,
            _unitRepoMock.Object,
            _categoryRepoMock.Object,
            _groupFieldRepoMock.Object,
            _systemFieldRepoMock.Object,
            _calculatedFieldRepoMock.Object,
            _genericFieldRepoMock.Object,
            _costEstimateRepoMock.Object,
            _fieldFileRepoMock.Object,
            _calculationServiceMock.Object,
            _blobMock.Object,
            _cacheMock.Object,
            NullLogger<CostEstimateTemplateService>.Instance);

        _templateRepoMock.Setup(r => r.Insert(It.IsAny<CostEstimateTemplate>())).Returns(Task.CompletedTask);
        _templateRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _templateRepoMock.Setup(r => r.Update(It.IsAny<CostEstimateTemplate>())).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveCacheByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    // ─── CreateTemplateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateTemplateAsync_ValidInput_InsertsTemplateAndReturnsId()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();

        // Act
        Guid result = await _sut.CreateTemplateAsync(ownerId, "Nowy szablon", "Opis", CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _templateRepoMock.Verify(r => r.Insert(It.Is<CostEstimateTemplate>(t =>
            t.OwnerId == ownerId &&
            t.Name == "Nowy szablon" &&
            t.Description == "Opis" &&
            !t.IsDeleted)),
            Times.Once);
        _templateRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_NullDescription_InsertsTemplateWithNullDescription()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();

        // Act
        Guid result = await _sut.CreateTemplateAsync(ownerId, "Szablon", null, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _templateRepoMock.Verify(r => r.Insert(It.Is<CostEstimateTemplate>(t => t.Description == null)), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_SetsDefaultValues()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        CostEstimateTemplate? captured = null;
        _templateRepoMock
            .Setup(r => r.Insert(It.IsAny<CostEstimateTemplate>()))
            .Callback<CostEstimateTemplate>(t => captured = t)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateTemplateAsync(ownerId, "T", null, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.CanAddGroups.Should().BeTrue();
        captured.CanBranchGroups.Should().BeTrue();
        captured.AutoNumberGroups.Should().BeFalse();
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ─── DeleteTemplateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTemplateAsync_SetsIsDeletedAndDeletedAt()
    {
        // Arrange
        CostEstimateTemplate template = new() { Id = Guid.NewGuid(), Name = "Old" };

        // Act
        await _sut.DeleteTemplateAsync(template, CancellationToken.None);

        // Assert
        template.IsDeleted.Should().BeTrue();
        template.DeletedAt.Should().NotBeNull();
        template.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteTemplateAsync_CallsUpdateAndSaveChanges()
    {
        // Arrange
        CostEstimateTemplate template = new() { Id = Guid.NewGuid() };

        // Act
        await _sut.DeleteTemplateAsync(template, CancellationToken.None);

        // Assert
        _templateRepoMock.Verify(r => r.Update(template), Times.Once);
        _templateRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplateAsync_InvalidatesCache()
    {
        // Arrange
        Guid templateId = Guid.NewGuid();
        CostEstimateTemplate template = new() { Id = templateId };

        // Act
        await _sut.DeleteTemplateAsync(template, CancellationToken.None);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            It.Is<string>(k => k.Contains(templateId.ToString())),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── InvalidateTemplateCacheAsync ─────────────────────────────────────────

    [Fact]
    public async Task InvalidateTemplateCacheAsync_RemovesCorrectCacheKey()
    {
        // Arrange
        Guid templateId = Guid.NewGuid();
        string expectedKey = $"platform:template:{templateId}";

        // Act
        await _sut.InvalidateTemplateCacheAsync(templateId, CancellationToken.None);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── UpdateTemplateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesTemplateProperties()
    {
        // Arrange
        CostEstimateTemplate template = new() { Id = Guid.NewGuid(), Name = "Old" };

        // Act
        await _sut.UpdateTemplateAsync(
            template,
            name: "New Name",
            description: "New Desc",
            category: "Cat",
            canAddGroups: false,
            canBranchGroups: false,
            maxGroupLevel: 3,
            autoNumberGroups: true,
            groupNumberFormat: "{n}",
            updateStructure: false,
            units: null,
            categories: null,
            groupHeaderFields: null,
            systemFields: null,
            calculatedFields: null,
            genericFields: null,
            uiConfiguration: null,
            cancellationToken: CancellationToken.None);

        // Assert
        template.Name.Should().Be("New Name");
        template.Description.Should().Be("New Desc");
        template.Category.Should().Be("Cat");
        template.CanAddGroups.Should().BeFalse();
        template.MaxGroupLevel.Should().Be(3);
        template.AutoNumberGroups.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTemplateAsync_SavesChangesAndInvalidatesCache()
    {
        // Arrange
        Guid templateId = Guid.NewGuid();
        CostEstimateTemplate template = new() { Id = templateId };

        // Act
        await _sut.UpdateTemplateAsync(
            template, "N", null, null, true, true, null, false, null,
            updateStructure: false,
            units: null, categories: null,
            groupHeaderFields: null, systemFields: null,
            calculatedFields: null, genericFields: null,
            uiConfiguration: null,
            cancellationToken: CancellationToken.None);

        // Assert
        _templateRepoMock.Verify(r => r.Update(template), Times.Once);
        _templateRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            It.Is<string>(k => k.Contains(templateId.ToString())),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
