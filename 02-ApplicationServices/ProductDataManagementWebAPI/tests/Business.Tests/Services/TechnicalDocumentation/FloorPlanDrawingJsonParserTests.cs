using System.Text.Json;
using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class FloorPlanDrawingJsonParserTests
{
    private static readonly JsonSerializerOptions JsonOptions = TechnicalDocumentationJsonHelper.CreateSerializerOptions();

    [Fact]
    public void Parse_foundationsAsArray_doesNotThrow()
    {
        string json = """
            {
              "foundations": [
                {"lengthM": 10, "widthM": 0.6, "heightM": 0.4, "concreteClass": "C20/25"}
              ],
              "rooms": [{"name": "Salon", "areaM2": "21.3"}]
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Foundations.Should().NotBeNull();
        drawing.Foundations!.Footings.Should().HaveCount(1);
        drawing.Foundations.Footings[0].LengthM.Should().Be(10);
    }

    [Fact]
    public void Parse_foundationsAsEmptyObject_returnsNull()
    {
        string json = """{"foundations": {}}""";

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Foundations.Should().BeNull();
    }

    [Fact]
    public void Parse_roofAsNull_doesNotThrow()
    {
        string json = """{"roof": null, "openings": {"type": "okno", "widthCm": "120", "heightCm": 140, "count": "2"}}""";

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Roof.Should().BeNull();
        drawing.Openings.Should().HaveCount(1);
        drawing.Openings[0].WidthCm.Should().Be(120);
        drawing.Openings[0].Count.Should().Be(2);
    }

    [Fact]
    public void Parse_installationsAsObject_mapsToList()
    {
        string json = """
            {
              "installations": {
                "electrical": {"isPresent": true, "notes": "trasy w suficie"},
                "heating": {"isPresent": false}
              }
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Installations.Should().HaveCount(2);
        drawing.Installations.Should().Contain(i => i.Type == "elektryczna" && i.IsPresent);
    }

    [Fact]
    public void Parse_validationReportWithNumericValues_doesNotThrow()
    {
        string json = """
            {
              "rooms": [],
              "validationReport": {
                "totalFields": 5,
                "agreeingFields": 3,
                "disagreementsResolved": 1,
                "lowConfidenceFields": 1,
                "disagreements": [{
                  "fieldPath": "rooms[0].dimensions.widthM",
                  "valueFromAgentA": 5.2,
                  "valueFromAgentB": "5.0",
                  "resolvedValue": 5.2,
                  "confidence": "medium"
                }]
              }
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.ValidationReport.Should().NotBeNull();
        drawing.ValidationReport!.Disagreements[0].ValueA.Should().Be("5.2");
        drawing.ValidationReport.Disagreements[0].ValueB.Should().Be("5.0");
    }

    [Fact]
    public void Parse_decimalWithoutLeadingZero_doesNotThrow()
    {
        string json = """
            {
              "rooms": [
                {"name": "Salon", "dimensions": {"widthM": .5, "lengthM": 4.1, "areaM2": 2.05}}
              ],
              "walls": [{"type": "zewnetrzna", "lengthM": -.75}]
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Rooms.Should().HaveCount(1);
        drawing.Rooms[0].WidthM.Should().Be(0.5);
        drawing.Walls.Should().HaveCount(1);
        drawing.Walls[0].LengthM.Should().Be(-0.75);
    }

    [Fact]
    public void Parse_ellipsisAndLoneDotPlaceholders_doesNotThrow()
    {
        string json = """
            {
              "rooms": [{"name": "Kuchnia", "symbol": ...}],
              "walls": [{"type": "wewnetrzna", "lengthM": .}]
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Rooms.Should().HaveCount(1);
        drawing.Rooms[0].Symbol.Should().BeEmpty();
        drawing.Walls.Should().HaveCount(1);
        drawing.Walls[0].LengthM.Should().Be(0);
    }

    [Fact]
    public void Parse_trailingCommasInArrays_doesNotThrow()
    {
        string json = """
            {
              "rooms": [
                {"name": "Salon", "areaM2": 21.3},
              ],
              "walls": [
                {"type": "zewnetrzna", "lengthM": 5.2},
              ],
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Rooms.Should().HaveCount(1);
        drawing.Rooms[0].Name.Should().Be("Salon");
        drawing.Walls.Should().HaveCount(1);
        drawing.Walls[0].Type.Should().Be("zewnetrzna");
    }

    [Fact]
    public void Serialize_roundTrip_doesNotStackOverflow()
    {
        FloorPlanDrawing drawing = new()
        {
            Foundations = new FoundationSection
            {
                Footings = [new FootingDetail { LengthM = 10, WidthM = 0.6, HeightM = 0.4 }],
                Blocks = [new MaterialQuantity { MaterialType = "bloczek", Quantity = 100, Unit = "szt" }]
            },
            Roof = new RoofSection { AreaM2 = 120, PitchDegrees = 35, CoveringType = "dachówka" },
            Installations = [new DrawingInstallation { Type = "elektryczna", IsPresent = true }]
        };

        string json = JsonSerializer.Serialize(drawing, JsonOptions);
        FloorPlanDrawing roundTrip = JsonSerializer.Deserialize<FloorPlanDrawing>(json, JsonOptions)!;

        roundTrip.Foundations.Should().NotBeNull();
        roundTrip.Foundations!.Footings.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_detailsWithReinforcementArray_doesNotThrow()
    {
        string json = """
            {
              "details": [
                {
                  "title": "Zbrojenie słupów",
                  "reinforcement": ["4#12", "strzemiona #6 co 20cm"]
                }
              ],
              "columns": [
                {
                  "symbol": "S-1",
                  "bCm": 24,
                  "hCm": 24,
                  "reinforcement": {"longitudinal": "4#12", "stirrups": "#6/20"}
                }
              ]
            }
            """;

        FloorPlanDrawing drawing = Deserialize(json);

        drawing.Details.Should().HaveCount(1);
        drawing.Details[0].Reinforcement.Should().Be("4#12; strzemiona #6 co 20cm");
        drawing.Columns.Should().HaveCount(1);
        drawing.Columns[0].LongitudinalBars.Should().Be("longitudinal=4#12; stirrups=#6/20");
    }

    private static FloorPlanDrawing Deserialize(string json)
    {
        string sanitized = TechnicalDocumentationJsonHelper.ExtractJson(json);
        return JsonSerializer.Deserialize<FloorPlanDrawing>(sanitized, JsonOptions)
            ?? new FloorPlanDrawing();
    }
}
