using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ExtractionFocusPromptLoaderTests
{
    [Theory]
    [InlineData("rzut_parteru", "Zestawienie pomieszczeń")]
    [InlineData("rzut fundamentow", "foundations")]
    [InlineData("przekroj", "sekcja section")]
    [InlineData("rzut wiezby dachowej", "Lista drewna")]
    public void GetPrompts_loadsMultilineFocusBlocks(string drawingType, string expectedSnippet)
    {
        // Act
        (string focusA, string focusB) = ExtractionFocusPromptLoader.GetPrompts(
            ExtractionFocusRouter.NormalizeDrawingType(drawingType));

        // Assert
        focusA.Should().Contain(expectedSnippet);
        focusB.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetPrompts_floorPlanFocusB_verifiesAreaSums()
    {
        // Act
        (string _, string focusB) = ExtractionFocusPromptLoader.GetPrompts("rzut parteru");

        // Assert
        focusB.Should().Contain("totalAreaM2");
        focusB.Should().NotContain("odwrotna");
    }
}
