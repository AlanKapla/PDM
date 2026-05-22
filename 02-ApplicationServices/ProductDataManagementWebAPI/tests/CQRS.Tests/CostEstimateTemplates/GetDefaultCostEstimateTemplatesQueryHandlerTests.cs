using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplates;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class GetDefaultCostEstimateTemplatesQueryHandlerTests
{
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly GetDefaultCostEstimateTemplatesQueryHandler _handler;

    public GetDefaultCostEstimateTemplatesQueryHandlerTests()
    {
        _handler = new GetDefaultCostEstimateTemplatesQueryHandler(
            _templateServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDefaultTemplatesExist_ReturnsList()
    {
        // Arrange
        List<DefaultCostEstimateTemplateListItemWeb> templates = new List<DefaultCostEstimateTemplateListItemWeb>
        {
            new DefaultCostEstimateTemplateListItemWeb("slug-1", "Template 1", null, "Construction"),
            new DefaultCostEstimateTemplateListItemWeb("slug-2", "Template 2", "Description", null)
        };

        GetDefaultCostEstimateTemplatesQuery query = new();

        _templateServiceMock
            .Setup(s => s.GetDefaultTemplates())
            .Returns(templates);

        // Act
        List<DefaultCostEstimateTemplateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenNoDefaultTemplates_ReturnsEmptyList()
    {
        // Arrange
        GetDefaultCostEstimateTemplatesQuery query = new();

        _templateServiceMock
            .Setup(s => s.GetDefaultTemplates())
            .Returns(new List<DefaultCostEstimateTemplateListItemWeb>());

        // Act
        List<DefaultCostEstimateTemplateListItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
