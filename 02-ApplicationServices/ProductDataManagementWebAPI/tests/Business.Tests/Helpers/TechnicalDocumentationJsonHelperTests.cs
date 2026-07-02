using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Helpers;

public sealed class TechnicalDocumentationJsonHelperTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Theory]
    [InlineData("```json\n{\"drawingType\":\"plan\"}\n```", "plan")]
    [InlineData("Oto wynik:\n```json\n{\"drawingType\":\"rzut\"}\n```", "rzut")]
    [InlineData("{\"drawingType\":\"elewacja\"}", "elewacja")]
    [InlineData("`{\"drawingType\":\"detal\"}`", "detal")]
    public void ExtractJson_markdownWrapped_parsesSuccessfully(string response, string expectedType)
    {
        string json = TechnicalDocumentationJsonHelper.ExtractJson(response);

        json.Should().StartWith("{");
        json.Should().NotContain("```");

        DrawingClassification? classification = JsonSerializer.Deserialize<DrawingClassification>(json, JsonOptions);
        classification.Should().NotBeNull();
        classification!.DrawingType.Should().Be(expectedType);
    }

    [Fact]
    public void ExtractJson_codeFenceWithoutBraces_returnsEmptyObject()
    {
        string json = TechnicalDocumentationJsonHelper.ExtractJson("```json\nbrak danych\n```");

        json.Should().Be("{}");
    }

    [Fact]
    public void ExtractJson_leadingBacktickWithoutBraces_returnsEmptyObject()
    {
        string json = TechnicalDocumentationJsonHelper.ExtractJson("```json");

        json.Should().Be("{}");
    }

    [Fact]
    public void DeserializeAgentResponse_invalidPayload_returnsFallback()
    {
        DrawingClassification fallback = new() { DrawingType = "nieznany" };

        DrawingClassification result = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
            "```json\nniepoprawny\n```",
            JsonOptions,
            fallback,
            context: "DrawingClassification");

        result.DrawingType.Should().Be("nieznany");
    }

    [Fact]
    public void DeserializeAgentResponse_markdownWrappedFloorPlan_doesNotThrow()
    {
        string response = """
            ```json
            {
              "rooms": [{"name": "Salon", "areaM2": 21.3}]
            }
            ```
            """;

        FloorPlanDrawing result = TechnicalDocumentationJsonHelper.DeserializeAgentResponse(
            response,
            JsonOptions,
            new FloorPlanDrawing(),
            context: "FloorPlanDrawing");

        result.Rooms.Should().HaveCount(1);
        result.Rooms[0].Name.Should().Be("Salon");
    }
}
