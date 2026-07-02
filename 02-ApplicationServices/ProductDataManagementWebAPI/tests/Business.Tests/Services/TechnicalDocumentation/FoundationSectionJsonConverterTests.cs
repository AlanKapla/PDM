using Business.Implementation.Helpers;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;
using System.Text.Json;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class FoundationSectionJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Fact]
    public void ParseFooting_parsesSegments_andSumsLengthM()
    {
        // Arrange
        string json = """
            {
              "foundations": {
                "footings": [
                  {
                    "symbol": "Ł-1",
                    "widthM": 0.60,
                    "heightM": 0.40,
                    "segments": [
                      {"id": "ściana N", "lengthM": 8.62},
                      {"id": "ściana S", "lengthM": 8.62}
                    ]
                  }
                ]
              }
            }
            """;

        // Act
        FloorPlanDrawing drawing = JsonSerializer.Deserialize<FloorPlanDrawing>(json, JsonOptions)!;

        // Assert
        FootingDetail footing = drawing.Foundations!.Footings.Single();
        footing.Segments.Should().HaveCount(2);
        footing.LengthM.Should().BeApproximately(17.24, 0.01);
    }

    [Fact]
    public void ResolveFootingLinearMetersFromFootings_sumsSegmentLengths()
    {
        // Arrange
        List<FootingDetail> footings =
        [
            new FootingDetail
            {
                Symbol = "Ł-1",
                Segments =
                [
                    new FootingSegmentDetail { Id = "N", LengthM = 8.62 },
                    new FootingSegmentDetail { Id = "S", LengthM = 8.62 }
                ]
            }
        ];

        // Act
        double linearM = MaterialBuildingGeometryHelper.ResolveFootingLinearMetersFromFootings(footings);

        // Assert
        linearM.Should().BeApproximately(17.2, 0.1);
    }
}
