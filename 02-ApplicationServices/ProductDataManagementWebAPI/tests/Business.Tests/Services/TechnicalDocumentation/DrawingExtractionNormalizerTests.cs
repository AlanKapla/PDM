using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DrawingExtractionNormalizerTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Fact]
    public void Parse_timberGroups_flattensToRoofTimber()
    {
        string json = """
            {
              "roof": {
                "timberGroups": [
                  {
                    "name": "Krokwie",
                    "section": "8x20",
                    "rows": [{"count": 2, "lengthM": 1.5, "rowSumMb": 3.0}],
                    "groupSumMb": 3.0,
                    "groupVolumeM3": 0.48
                  }
                ],
                "totalVolumeM3": 0.48
              }
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);
        DrawingExtractionNormalizer.Normalize(drawing, new DrawingClassification());

        drawing.Roof!.Timber.Should().HaveCount(1);
        drawing.Roof.Timber[0].Element.Should().Be("Krokwie");
        drawing.Roof.Timber[0].Section.Should().Be("8x20");
        drawing.Roof.TotalVolumeM3.Should().Be(0.48);
    }

    [Fact]
    public void Parse_foundationPads_mapsToFoundationsSection()
    {
        string json = """
            {
              "foundations": {
                "concreteClass": "C20/25 (B25)",
                "pads": [{"bM": 1.3, "lM": 1.0, "heightM": 0.45}]
              }
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);
        DrawingExtractionNormalizer.Normalize(drawing, new DrawingClassification());

        drawing.Foundations!.Pads.Should().HaveCount(1);
        drawing.Foundations.ConcreteClass.Should().Be("C20/25 (B25)");
    }

    [Fact]
    public void Parse_rebarBars_computesTotalMassKg()
    {
        string json = """
            {
              "floors": {
                "bars": [
                  {"pos": 1, "massKg": 100.5},
                  {"pos": 2, "massKg": 50.0}
                ]
              }
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);
        DrawingExtractionNormalizer.Normalize(drawing, new DrawingClassification());

        drawing.Floors!.TotalMassKg.Should().Be(150.5);
        drawing.Floors.Steel.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_roomNumber_mapsToSymbol()
    {
        FloorPlanDrawing drawing = new()
        {
            Rooms = [new Room { Name = "Salon", Number = "12", AreaM2 = 20 }]
        };

        DrawingExtractionNormalizer.Normalize(drawing, new DrawingClassification());

        drawing.Rooms[0].Symbol.Should().Be("12");
    }

    [Fact]
    public void FallbackBuilder_mapsPadsColumnsAndSite()
    {
        FloorPlanDrawing drawing = new()
        {
            Classification = new DrawingClassification { FloorLevel = "Parter", FloorOrder = 0 },
            Site = new SitePlanSection { PlotAreaM2 = 720, BuildingFootprintM2 = 194.55 },
            Foundations = new FoundationSection
            {
                ConcreteClass = "C20/25",
                Pads = [new PadDetail { BM = 1.3, LM = 1.0, HeightM = 0.45 }]
            },
            Columns = [new StructuralColumn { Symbol = "S-1", BCm = 24, HCm = 24 }]
        };

        ProjectModel model = ProjectModelFallbackBuilder.Build([drawing]);

        model.Site.PlotAreaM2.Should().Be(720);
        model.Foundations.Pads.Should().HaveCount(1);
        model.Columns.Should().HaveCount(1);
    }

    private static FloorPlanDrawing Deserialize(string json)
    {
        string sanitized = TechnicalDocumentationJsonHelper.ExtractJson(json);
        FloorPlanDrawing drawing = JsonSerializer.Deserialize<FloorPlanDrawing>(sanitized, JsonOptions)
            ?? new FloorPlanDrawing();
        DrawingExtractionNormalizer.Normalize(drawing, new DrawingClassification());
        return drawing;
    }
}
