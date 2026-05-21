using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplateDetails;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class GetDefaultCostEstimateTemplateDetailsQueryHandlerTests
{
    private readonly Mock<ICostEstimateTemplateService> _templateServiceMock = new();
    private readonly GetDefaultCostEstimateTemplateDetailsQueryHandler _handler;

    public GetDefaultCostEstimateTemplateDetailsQueryHandlerTests()
    {
        _handler = new GetDefaultCostEstimateTemplateDetailsQueryHandler(
            _templateServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSlugExists_ReturnsStructure()
    {
        // Arrange
        string slug = "default-template";
        CostEstimateTemplateStructureWeb structure = new CostEstimateTemplateStructureWeb(
            Guid.NewGuid(),
            null,
            new List<UnitWeb>(),
            new List<CategoryWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            new List<FieldDefinitionWeb>(),
            null);

        GetDefaultCostEstimateTemplateDetailsQuery query = new(slug);

        _templateServiceMock
            .Setup(s => s.GetDefaultTemplateDetails(slug))
            .Returns(structure);

        // Act
        CostEstimateTemplateStructureWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(structure);
    }

    [Fact]
    public async Task Handle_WhenSlugNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        GetDefaultCostEstimateTemplateDetailsQuery query = new("non-existent-slug");

        _templateServiceMock
            .Setup(s => s.GetDefaultTemplateDetails(It.IsAny<string>()))
            .Returns((CostEstimateTemplateStructureWeb?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
