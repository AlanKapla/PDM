using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DrawingMaterialConsolidatorTests
{
    [Fact]
    public void Consolidate_usesMaxNotSum_forDuplicateFoundationConcreteAcrossPages()
    {
        List<FloorPlanDrawing> drawings =
        [
            CreateFoundationDrawing("rzut parteru", 12.0),
            CreateFoundationDrawing("przekrój A-A", 12.0),
            CreateFoundationDrawing("przekrój B-B", 11.5)
        ];

        ConsolidatedProjectMaterials consolidated = DrawingMaterialConsolidator.Consolidate(drawings, []);

        consolidated.FoundationConcrete.Should().HaveCount(1);
        consolidated.FoundationConcrete[0].Quantity.Should().Be(12.0);
        consolidated.AuditNotes.Should().NotBeEmpty();
    }

    [Fact]
    public void Consolidate_sumsWallMaterialsAcrossPlanFloors_butNotAcrossSections()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut parteru" },
                Walls =
                [
                    new Wall
                    {
                        NetAreaM2 = 40,
                        Layers = [new WallLayer { Material = "beton komórkowy", ThicknessCm = 24 }]
                    }
                ]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut piętra" },
                Walls =
                [
                    new Wall
                    {
                        NetAreaM2 = 30,
                        Layers = [new WallLayer { Material = "beton komórkowy", ThicknessCm = 24 }]
                    }
                ]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "przekrój 1-1" },
                Walls =
                [
                    new Wall
                    {
                        NetAreaM2 = 100,
                        Layers = [new WallLayer { Material = "beton komórkowy", ThicknessCm = 24 }]
                    }
                ]
            }
        ];

        ConsolidatedProjectMaterials consolidated = DrawingMaterialConsolidator.Consolidate(drawings, []);

        consolidated.WallMaterials.Should().HaveCount(1);
        consolidated.WallMaterials[0].Quantity.Should().Be(16.8);
    }

    [Fact]
    public void Consolidate_timberTakenOnceFromRoofDrawing()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut parteru" },
                Roof = new RoofSection
                {
                    Timber = [new TimberElement { Element = "krokwie", Section = "8x16", WoodType = "C24", Count = 8, LengthM = 4.5 }]
                }
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "dach" },
                Roof = new RoofSection
                {
                    AreaM2 = 120,
                    Timber = [new TimberElement { Element = "krokwie", Section = "8x16", WoodType = "C24", Count = 16, LengthM = 4.5 }]
                }
            }
        ];

        ConsolidatedProjectMaterials consolidated = DrawingMaterialConsolidator.Consolidate(drawings, []);

        consolidated.Timber.Should().HaveCount(1);
        consolidated.Timber[0].Quantity.Should().Be(0.922);
        consolidated.Timber[0].Unit.Should().Be("m3");
        consolidated.Timber[0].MaterialType.Should().Be("krokwie 8x16 C24");
    }

    private static FloorPlanDrawing CreateFoundationDrawing(string drawingType, double concreteQuantity)
    {
        return new FloorPlanDrawing
        {
            Classification = new DrawingClassification { DrawingType = drawingType },
            Foundations = new FoundationSection
            {
                Concrete =
                [
                    new MaterialQuantity { MaterialType = "C20/25", Quantity = concreteQuantity, Unit = "m3" }
                ]
            }
        };
    }
}
