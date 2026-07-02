using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class MaterialScheduleDrawingEnricherTests
{
    [Fact]
    public void Enrich_populatesAllBuildingElementCategories()
    {
        List<FloorPlanDrawing> drawings = CreateFullDrawingSet();
        ProjectModel projectModel = ProjectModelFallbackBuilder.Build(drawings);
        MaterialSchedule schedule = new();

        schedule = MaterialScheduleDrawingEnricher.Enrich(
            schedule,
            projectModel,
            drawings,
            new Dictionary<string, object> { ["ceiling.thicknessCm"] = 18.0, ["roof.areaM2"] = 328.0 });

        schedule.Foundations.Concrete.Should().NotBeEmpty();
        schedule.Foundations.Blocks.Should().NotBeEmpty();
        schedule.Foundations.Steel.Should().NotBeEmpty();
        schedule.Walls.Masonry.Should().NotBeEmpty();
        schedule.Walls.Insulation.Should().NotBeEmpty();
        schedule.Ceilings.Steel.Should().HaveCount(2);
        schedule.Ceilings.Concrete.Should().NotBeEmpty();
        schedule.Roof.Covering.Should().NotBeEmpty();
        schedule.Roof.Insulation.Should().NotBeEmpty();
        schedule.Insulation.Should().NotBeEmpty();
        schedule.Masonry.Should().NotBeEmpty();
        schedule.Concrete.Should().NotBeEmpty();
        schedule.Steel.Should().NotBeEmpty();
    }

    [Fact]
    public void Consolidate_includesSteelFromReinforcementDrawings()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "zbrojenie_stropu_dolne" },
                Floors = new FloorSection
                {
                    Steel = [new MaterialQuantity { MaterialType = "stal zbrojeniowa", Quantity = 1170.3, Unit = "kg" }]
                }
            }
        ];

        ConsolidatedProjectMaterials consolidated = DrawingMaterialConsolidator.Consolidate(drawings, []);

        consolidated.FloorSteel.Should().HaveCount(1);
        consolidated.FloorSteel[0].Quantity.Should().Be(1170.3);
    }

    [Fact]
    public void GeometryHelper_estimatesPerimeterFromFootprint()
    {
        BuildingGeometry geometry = MaterialBuildingGeometryHelper.Resolve(
            new ProjectModel
            {
                Floors =
                [
                    new ProjectModelFloor
                    {
                        Level = "Parter",
                        TotalAreaM2 = 150,
                        Rooms = [new ProjectModelRoom { Name = "Salon", AreaM2 = 150 }]
                    }
                ]
            },
            [],
            new Dictionary<string, object>());

        geometry.FootprintM2.Should().Be(150);
        geometry.PerimeterM.Should().BeGreaterThan(40);
    }

    private static List<FloorPlanDrawing> CreateFullDrawingSet()
    {
        return
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "rzut_parteru",
                    SheetNumber = "A-02",
                    FloorLevel = "Parter"
                },
                Rooms =
                [
                    new Room { Name = "Salon", AreaM2 = 40 },
                    new Room { Name = "Garaż", AreaM2 = 37 }
                ],
                Walls =
                [
                    new Wall
                    {
                        Type = "zewnętrzna",
                        ThicknessCm = 44,
                        Layers =
                        [
                            new WallLayer { Material = "beton komórkowy", ThicknessCm = 24 },
                            new WallLayer { Material = "styropian EPS", ThicknessCm = 20 }
                        ]
                    }
                ]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "rzut_poddasza",
                    SheetNumber = "A-03",
                    FloorLevel = "Poddasze"
                },
                Rooms = [new Room { Name = "Pokój", AreaM2 = 15 }]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "przekroj",
                    SheetNumber = "A-05"
                },
                Section = new SectionDrawingData
                {
                    Levels = new SectionLevels { FoundationBottomM = -1.32, GroundFloorM = 0, CeilingM = 2.88 },
                    FloorZones =
                    [
                        new SectionZone
                        {
                            Zone = "A",
                            Layers = [new WallLayer { Material = "styropian EPS 100", ThicknessCm = 15 }]
                        }
                    ]
                }
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "rzut_fundamentow",
                    SheetNumber = "K-01"
                },
                Foundations = new FoundationSection
                {
                    ConcreteClass = "B25",
                    Footings =
                    [
                        new FootingDetail { Symbol = "Ł-1", LengthM = 0.6, WidthM = 0.6, HeightM = 0.4 }
                    ]
                }
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "zbrojenie_stropu_dolne",
                    SheetNumber = "K-02"
                },
                Floors = new FloorSection
                {
                    Steel = [new MaterialQuantity { MaterialType = "stal zbrojeniowa", Quantity = 1170.3, Unit = "kg" }],
                    Slabs = [new SlabDetail { ThicknessCm = 18, ConcreteClass = "C20/25 (B25)" }]
                }
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification
                {
                    DrawingType = "zbrojenie_stropu_gorne",
                    SheetNumber = "K-03"
                },
                Floors = new FloorSection
                {
                    Steel = [new MaterialQuantity { MaterialType = "stal zbrojeniowa", Quantity = 604.73, Unit = "kg" }]
                }
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut_dachu", SheetNumber = "A-04" },
                Roof = new RoofSection { AreaM2 = 328, CoveringType = "dachówka", PitchDegrees = 35 }
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut_wiezby_dachowej", SheetNumber = "K-04" },
                Roof = new RoofSection
                {
                    Timber =
                    [
                        new TimberElement { Element = "Krokwie", Section = "10x20", Count = 10, LengthM = 5, WoodType = "C24" }
                    ]
                }
            }
        ];
    }
}
