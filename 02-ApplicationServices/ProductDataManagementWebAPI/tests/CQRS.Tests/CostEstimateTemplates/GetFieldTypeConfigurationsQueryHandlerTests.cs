using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetFieldTypeConfigurations;
using FluentAssertions;

namespace CQRS.Tests.CostEstimateTemplates;

public sealed class GetFieldTypeConfigurationsQueryHandlerTests
{
    private readonly GetFieldTypeConfigurationsQueryHandler _handler = new();

    [Fact]
    public async Task Handle_Always_ReturnsNonEmptyDictionary()
    {
        // Arrange
        GetFieldTypeConfigurationsQuery query = new();

        // Act
        Dictionary<int, CostEstimateFieldTypeConfigWeb[]> result =
            await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Always_AllConfigsHaveNonEmptyItems()
    {
        // Arrange
        GetFieldTypeConfigurationsQuery query = new();

        // Act
        Dictionary<int, CostEstimateFieldTypeConfigWeb[]> result =
            await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().OnlyContain(kvp => kvp.Value.Length > 0);
    }
}
