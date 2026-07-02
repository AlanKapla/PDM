using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DrawingClassificationJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Theory]
    [InlineData("""{"drawingType":"rzut","scale":100}""", 100)]
    [InlineData("""{"drawingType":"rzut","scale":"1:100"}""", 100)]
    [InlineData("""{"drawingType":"rzut","scale":"100"}""", 100)]
    [InlineData("""{"drawingType":"rzut","scale":null}""", null)]
    public void Deserialize_scale_acceptsNumberAndStringFormats(string json, int? expectedScale)
    {
        DrawingClassification? classification = JsonSerializer.Deserialize<DrawingClassification>(json, JsonOptions);

        classification.Should().NotBeNull();
        classification!.Scale.Should().Be(expectedScale);
    }

    [Fact]
    public void Deserialize_technicalParameters_object_formatsAsKeyValuePairs()
    {
        // Arrange
        string json = """
            {
              "drawingType": "rzut_poddasza",
              "technicalParameters": {
                "concrete": "C20/25 (B25)",
                "steel": "RB500",
                "externalWallThicknessCm": 44
              }
            }
            """;

        // Act
        DrawingClassification? classification = JsonSerializer.Deserialize<DrawingClassification>(json, JsonOptions);

        // Assert
        classification.Should().NotBeNull();
        classification!.TechnicalParameters.Should().Be("concrete=C20/25 (B25); steel=RB500; externalWallThicknessCm=44");
    }

    [Fact]
    public void Deserialize_technicalParameters_string_preservesPlainText()
    {
        // Arrange
        string json = """{"drawingType":"przekroj","technicalParameters":"Beton C20/25"}""";

        // Act
        DrawingClassification? classification = JsonSerializer.Deserialize<DrawingClassification>(json, JsonOptions);

        // Assert
        classification.Should().NotBeNull();
        classification!.TechnicalParameters.Should().Be("Beton C20/25");
    }

    [Fact]
    public void Deserialize_technicalParameters_nullOrEmptyObject_leavesPropertyNull()
    {
        // Arrange
        string jsonWithNull = """{"drawingType":"przekroj","technicalParameters":null}""";
        string jsonWithEmptyObject = """{"drawingType":"przekroj","technicalParameters":{}}""";

        // Act
        DrawingClassification? fromNull = JsonSerializer.Deserialize<DrawingClassification>(jsonWithNull, JsonOptions);
        DrawingClassification? fromEmptyObject = JsonSerializer.Deserialize<DrawingClassification>(jsonWithEmptyObject, JsonOptions);

        // Assert
        fromNull!.TechnicalParameters.Should().BeNull();
        fromEmptyObject!.TechnicalParameters.Should().BeNull();
    }
}
