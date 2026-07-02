using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialScheduleJsonParserTests
{
    [Fact]
    public void Parse_emptyCalculatedAt_parsesSummary()
    {
        string response = """
            {
              "drawingsUsed": ["plan.pdf"],
              "foundations": {"concrete":[],"steel":[],"blocks":[]},
              "walls": {"masonry":[],"mortar":[],"insulation":[]},
              "ceilings": {"concrete":[],"steel":[]},
              "columns": {"concrete":[],"steel":[]},
              "roof": {"covering":[],"timber":[],"insulation":[]},
              "openings": [],
              "summary": [{"category":"beton","materialType":"C25/30","grossQuantity":8.5,"unit":"m3"}],
              "calculatedAt": "",
              "assumptions": [],
              "warnings": []
            }
            """;

        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Source = new DrawingSource { FileName = "plan.pdf", PageNumber = 1 }
            }
        ];

        MaterialSchedule schedule = MaterialScheduleJsonParser.Parse(response, drawings, "jednorodzinny");

        schedule.Summary.Should().NotBeEmpty();
        schedule.CalculatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Parse_flatCategorySchema_deserializesMasonryAndMissingDimensions()
    {
        string response = """
            {
              "drawingsUsed": ["A-02"],
              "missingDimensions": ["długość ławy Ł-1"],
              "masonry": [{
                "element": "Ściany zewnętrzne",
                "calculation": "38mb × 2.80m = 106.4m2",
                "sourceType": "calculated",
                "sourceDrawings": ["A-02 Rzut parteru"],
                "netQuantity": 21.6,
                "wastePercent": 5,
                "grossQuantity": 22.68,
                "unit": "m3"
              }],
              "insulation": [],
              "concrete": [],
              "steel": [],
              "timber": [],
              "roofing": [],
              "finishes": [],
              "openings": [],
              "summary": [],
              "assumptions": [],
              "warnings": []
            }
            """;

        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Source = new DrawingSource { FileName = "A-02.pdf", PageNumber = 1 }
            }
        ];

        MaterialSchedule schedule = MaterialScheduleJsonParser.Parse(response, drawings, "jednorodzinny");

        schedule.Masonry.Should().ContainSingle();
        schedule.Masonry[0].Element.Should().Be("Ściany zewnętrzne");
        schedule.Masonry[0].SourceType.Should().Be("calculated");
        schedule.MissingDimensions.Should().Contain("długość ławy Ł-1");
    }

    [Fact]
    public void Parse_invalidJson_returnsFallbackWithDrawingLabels()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Source = new DrawingSource { FileName = "rzut.pdf", PageNumber = 2 },
                Foundations = new FoundationSection
                {
                    Blocks = [new MaterialQuantity { MaterialType = "bloczek", Quantity = 100, Unit = "szt" }]
                }
            }
        ];

        MaterialSchedule schedule = MaterialScheduleJsonParser.Parse("```json\nniepoprawny\n```", drawings, "jednorodzinny");

        schedule.DrawingsUsed.Should().ContainSingle(label => label.Contains("rzut.pdf"));
        schedule.Summary.Should().NotBeEmpty();
        schedule.Warnings.Should().NotBeEmpty();
    }
}
