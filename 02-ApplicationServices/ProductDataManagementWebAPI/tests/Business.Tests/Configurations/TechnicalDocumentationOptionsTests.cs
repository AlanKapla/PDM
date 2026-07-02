using Business.Interfaces.Configurations;
using FluentAssertions;

namespace Business.Tests.Configurations;

public sealed class TechnicalDocumentationOptionsTests
{
    [Fact]
    public void DefaultValues_MatchPipelineReimplementationSpec()
    {
        TechnicalDocumentationOptions options = new();

        options.UseGroupPipeline.Should().BeFalse();
        options.MaxImagesPerGroup.Should().Be(6);
        options.CompressionThresholdBytes.Should().Be(3_145_728);
        options.DrawingTypeToThematicGroups.Should().BeEmpty();
    }

    [Fact]
    public void GetEffectiveDrawingTypeToThematicGroups_WhenConfigEmpty_ReturnsDefaults()
    {
        TechnicalDocumentationOptions options = new();

        IReadOnlyDictionary<string, string[]> mapping = options.GetEffectiveDrawingTypeToThematicGroups();

        mapping.Should().ContainKey(TechnicalDocumentationOptions.DrawingTypes.DetaleKonstrukcyjne);
        mapping[TechnicalDocumentationOptions.DrawingTypes.DetaleKonstrukcyjne]
            .Should()
            .BeEquivalentTo(
            [
                TechnicalDocumentationOptions.ThematicGroups.Reinforcement,
                TechnicalDocumentationOptions.ThematicGroups.Foundations,
            ]);
    }

    [Fact]
    public void GetEffectiveDrawingTypeToThematicGroups_WhenConfigProvided_UsesConfig()
    {
        TechnicalDocumentationOptions options = new()
        {
            DrawingTypeToThematicGroups = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [TechnicalDocumentationOptions.DrawingTypes.RzutParteru] =
                    [TechnicalDocumentationOptions.ThematicGroups.Site],
            },
        };

        IReadOnlyDictionary<string, string[]> mapping = options.GetEffectiveDrawingTypeToThematicGroups();

        mapping.Should().ContainKey(TechnicalDocumentationOptions.DrawingTypes.RzutParteru);
        mapping[TechnicalDocumentationOptions.DrawingTypes.RzutParteru]
            .Should()
            .BeEquivalentTo([TechnicalDocumentationOptions.ThematicGroups.Site]);
        mapping.Should().NotContainKey(TechnicalDocumentationOptions.DrawingTypes.DetaleKonstrukcyjne);
    }
}
