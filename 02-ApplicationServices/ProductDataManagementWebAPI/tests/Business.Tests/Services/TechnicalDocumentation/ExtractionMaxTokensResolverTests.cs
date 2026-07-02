using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ExtractionMaxTokensResolverTests
{
    [Theory]
    [InlineData("rzut_wiezby_dachowej", 12000)]
    [InlineData("zbrojenie_stropu_dolne", 6000)]
    [InlineData("zbrojenie_stropu_gorne", 6000)]
    [InlineData("rzut_parteru", 4096)]
    public void Resolve_returnsExpectedBudget(string drawingType, int expected)
    {
        int result = ExtractionMaxTokensResolver.Resolve(drawingType);

        result.Should().Be(expected);
    }
}

public sealed class FloorReinforcementDrawingResolverTests
{
    [Fact]
    public void Resolve_mapsBottomAndTopByDrawingType()
    {
        List<FloorPlanDrawing> drawings =
        [
            CreateDrawing("zbrojenie_stropu_dolne", "K-02", CreateFloorSection(100)),
            CreateDrawing("zbrojenie_stropu_gorne", "K-03", CreateFloorSection(200))
        ];

        FloorReinforcementDrawingResolver.ReinforcementLayers layers =
            FloorReinforcementDrawingResolver.Resolve(drawings);

        layers.Bottom.Should().NotBeNull();
        layers.Top.Should().NotBeNull();
        layers.Bottom!.TotalMassKg.Should().Be(100);
        layers.Top!.TotalMassKg.Should().Be(200);
        layers.BottomSheet.Should().Be("K-02");
        layers.TopSheet.Should().Be("K-03");
    }

    [Fact]
    public void Resolve_usesSheetFallbackWhenTypesAreAmbiguous()
    {
        List<FloorPlanDrawing> drawings =
        [
            CreateDrawing("nieznany", "K-02", CreateFloorSection(111)),
            CreateDrawing("nieznany", "K-03", CreateFloorSection(222))
        ];

        FloorReinforcementDrawingResolver.ReinforcementLayers layers =
            FloorReinforcementDrawingResolver.Resolve(drawings);

        layers.Bottom!.TotalMassKg.Should().Be(111);
        layers.Top!.TotalMassKg.Should().Be(222);
    }

    [Fact]
    public void Resolve_detectsReinforcementFromSteelList()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "zbrojenie_stropu_dolne",
                    SheetNumber = "K-02"
                },
                Floors = new FloorSection
                {
                    Steel =
                    [
                        new MaterialQuantity
                        {
                            MaterialType = "stal zbrojeniowa",
                            Quantity = 845.5,
                            Unit = "kg"
                        }
                    ]
                }
            }
        ];

        FloorReinforcementDrawingResolver.ReinforcementLayers layers =
            FloorReinforcementDrawingResolver.Resolve(drawings);

        layers.Bottom.Should().NotBeNull();
        layers.Bottom!.TotalMassKg.Should().Be(845.5);
    }

    private static FloorPlanDrawing CreateDrawing(string drawingType, string sheetNumber, FloorSection floors)
    {
        return new FloorPlanDrawing
        {
            Classification = new DrawingClassification
            {
                DrawingType = drawingType,
                SheetNumber = sheetNumber
            },
            Floors = floors
        };
    }

    private static FloorSection CreateFloorSection(double totalMassKg)
    {
        return new FloorSection
        {
            TotalMassKg = totalMassKg,
            Bars = [new RebarBar { Pos = 1, MassKg = totalMassKg }]
        };
    }
}
