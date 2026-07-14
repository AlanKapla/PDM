using Business.Implementation.Services.AI;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace Business.Tests.Services.AI;

public sealed class AICostDocumentEnrichmentServiceTests
{
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<IReadRepository<ProjectCostCategory>> _categoryRepoMock = new();
    private readonly Mock<ILogger<AICostDocumentEnrichmentService>> _loggerMock = new();
    private readonly AICostDocumentEnrichmentService _sut;

    public AICostDocumentEnrichmentServiceTests()
    {
        _sut = new AICostDocumentEnrichmentService(
            _contractorServiceMock.Object,
            _categoryRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EnrichWithContractorAsync_WhenContractorFound_SetsContractorId()
    {
        // Arrange
        Guid contractorId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        ParsedCostDto input = new ParsedCostDto
        {
            Name = "Test",
            ContractorName = "ACME",
            ContractorNip = "1234567890",
            Confidence = 0.9
        };

        _contractorServiceMock
            .Setup(s => s.SearchByProfileAsync(
                input.ContractorName,
                input.ContractorNip,
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contractor
            {
                Id = contractorId,
                Name = "ACME",
                TenantId = tenantId
            });

        // Act
        ParsedCostDto result = await _sut.EnrichWithContractorAsync(input, tenantId, CancellationToken.None);

        // Assert
        result.ContractorId.Should().Be(contractorId);
        result.ContractorFound.Should().BeTrue();
        result.SuggestedContractor.Should().BeNull();
    }

    [Fact]
    public async Task EnrichWithContractorAsync_WhenNotFound_SuggestsNewContractor()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        ParsedCostDto input = new ParsedCostDto
        {
            Name = "Test",
            ContractorName = "New Co",
            ContractorNip = "9876543210",
            Confidence = 0.9
        };

        _contractorServiceMock
            .Setup(s => s.SearchByProfileAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contractor?)null);

        // Act
        ParsedCostDto result = await _sut.EnrichWithContractorAsync(input, tenantId, CancellationToken.None);

        // Assert
        result.ContractorFound.Should().BeFalse();
        result.SuggestedContractor.Should().NotBeNull();
        result.SuggestedContractor!.Name.Should().Be("New Co");
    }

    [Fact]
    public async Task EnrichWithCategoryAsync_WhenExactMatchFound_SetsCategoryId()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        ParsedCostDto input = new ParsedCostDto
        {
            Name = "Test",
            CategoryName = "Materiały",
            Confidence = 0.9
        };

        _categoryRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectCostCategory, bool>>>()))
            .ReturnsAsync(new List<ProjectCostCategory>
            {
                new ProjectCostCategory
                {
                    Id = categoryId,
                    ProjectId = projectId,
                    Name = "Materiały"
                }
            });

        // Act
        ParsedCostDto result = await _sut.EnrichWithCategoryAsync(input, projectId, CancellationToken.None);

        // Assert
        result.CategoryId.Should().Be(categoryId);
        result.CategoryFound.Should().BeTrue();
    }
}
