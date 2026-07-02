using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ExtractionFocusRouterTests
{
    private readonly ExtractionFocusRouter router = new();

    [Theory]
    [InlineData("rzut parteru", true)]
    [InlineData("rzut_parteru", true)]
    [InlineData("RZUT PARTERU", true)]
    [InlineData("rzut piętra", true)]
    [InlineData("rzut poddasza", true)]
    [InlineData("rzut piwnicy", true)]
    [InlineData("rzut fundamentów", true)]
    [InlineData("rzut dachu", false)]
    [InlineData("elewacja", false)]
    [InlineData("przekrój", false)]
    public void Resolve_criticalDrawingTypes_requireCrossValidation(string drawingType, bool expectedCv)
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = drawingType,
            HasMaterialTable = false
        };

        // Act
        ExtractionFocusRoute route = router.Resolve(classification);

        // Assert
        route.RequiresCrossValidation.Should().Be(expectedCv);
        route.FocusA.Should().NotBeNullOrWhiteSpace();
        route.FocusB.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Resolve_hasMaterialTable_requiresCrossValidation()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "elewacja",
            HasMaterialTable = true
        };

        // Act
        ExtractionFocusRoute route = router.Resolve(classification);

        // Assert
        route.RequiresCrossValidation.Should().BeTrue();
    }

    [Fact]
    public void Resolve_rzutParteru_mapsFocusPrompts()
    {
        // Arrange
        DrawingClassification classification = new()
        {
            DrawingType = "rzut parteru"
        };

        // Act
        ExtractionFocusRoute route = router.Resolve(classification);

        // Assert
        route.FocusA.Should().Contain("Zestawienie pomieszczeń");
        route.FocusB.Should().Contain("Otwory");
    }

    [Theory]
    [InlineData("rzut_parteru", "rzut parteru")]
    [InlineData("RZUT  PARTERU", "rzut parteru")]
    public void NormalizeDrawingType_replacesUnderscoresAndCase(string input, string expected)
    {
        // Act
        string normalized = ExtractionFocusRouter.NormalizeDrawingType(input);

        // Assert
        normalized.Should().Be(expected);
    }
}
