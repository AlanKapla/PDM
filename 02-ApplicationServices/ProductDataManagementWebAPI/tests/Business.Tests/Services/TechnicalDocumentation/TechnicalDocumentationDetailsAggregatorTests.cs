using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationDetailsAggregatorTests
{
    [Fact]
    public void Aggregate_mergesRoomsAndJoineryAcrossDrawings()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { Title = "Dom A", DrawingType = "rzut parteru" },
                Rooms = [new Room { Name = "Salon", AreaM2 = 20 }],
                Openings = [new Opening { Type = "okno", WidthCm = 120, HeightCm = 140, Count = 2 }]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut parteru" },
                Rooms = [new Room { Name = "Kuchnia", AreaM2 = 12 }],
                Openings = [new Opening { Type = "drzwi", WidthCm = 90, HeightCm = 210, Count = 1 }]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Project.Name.Should().Be("Dom A");
        details.Rooms.Should().HaveCount(1);
        details.Rooms[0].Items.Should().HaveCount(2);
        details.TotalAreaM2.Should().Be(32);
        details.Joinery!.Exterior!.Windows.Should().HaveCount(1);
        details.Joinery.Exterior.Doors.Should().HaveCount(1);
    }

    [Fact]
    public void Aggregate_groupsRoomsByFloorDrawing()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut parteru", Title = "Rzut parteru" },
                Rooms = [new Room { Name = "Garaż", Symbol = "G1", AreaM2 = 21 }]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut piętra", Title = "Rzut piętra" },
                Rooms = [new Room { Name = "Garaz", Symbol = "G2", AreaM2 = 21.5 }]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Rooms.Should().HaveCount(2);
        details.TotalAreaM2.Should().Be(42.5);
    }

    [Fact]
    public void Aggregate_usesMaxJoineryCountPerSymbolAcrossDrawings()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Openings =
                [
                    new Opening { Type = "okno", Symbol = "O1", WidthCm = 120, HeightCm = 140, Count = 2 }
                ]
            },
            new FloorPlanDrawing
            {
                Openings =
                [
                    new Opening { Type = "okno", Symbol = "O1", WidthCm = 120, HeightCm = 140, Count = 4 }
                ]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Joinery!.Exterior!.Windows.Should().HaveCount(1);
        details.Joinery.Exterior.Windows[0].Count.Should().Be(4);
    }

    [Fact]
    public void Aggregate_mapsExternalWallLayersFromProjectModel()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Walls =
                [
                    new Wall
                    {
                        NetAreaM2 = 50,
                        Layers =
                        [
                            new WallLayer { Material = "beton komórkowy", ThicknessCm = 24 }
                        ]
                    }
                ]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Walls!.External!.Layers.Should().HaveCount(1);
        details.Walls.External.Layers[0].Material.Should().Be("beton komórkowy");
    }

    [Fact]
    public void Aggregate_prefersLabeledRoomAreaOverCalculatedDimensions()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut parteru" },
                Rooms =
                [
                    new Room
                    {
                        Name = "Salon",
                        WidthM = 5.0,
                        LengthM = 4.0,
                        AreaM2 = 99
                    }
                ]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Rooms[0].Items[0].AreaM2.Should().Be(99);
        details.TotalAreaM2.Should().Be(99);
    }

    [Fact]
    public void Aggregate_keepsSameNamedRoomsOnDifferentFloors()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut parteru", Title = "Rzut parteru" },
                Rooms = [new Room { Name = "Łazienka", AreaM2 = 6 }]
            },
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut piętra", Title = "Rzut piętra" },
                Rooms = [new Room { Name = "Łazienka", AreaM2 = 5 }]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Rooms.Should().HaveCount(2);
        details.TotalAreaM2.Should().Be(11);
    }

    [Fact]
    public void Aggregate_collectsWallInsulationInThermalSummary()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Walls =
                [
                    new Wall
                    {
                        Layers =
                        [
                            new WallLayer { Material = "styropian XPS fundamentowy", ThicknessCm = 10 }
                        ]
                    }
                ]
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.ThermalInsulation.Should().NotBeNull();
        details.ThermalInsulation!.Elements.Should().HaveCount(1);
        details.ThermalInsulation.Elements[0].Material.Should().Contain("styropian");
    }

    [Fact]
    public void Aggregate_mapsFoundationPadsFromDrawing()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut_fundamentow", SheetNumber = "K-01" },
                Foundations = new FoundationSection
                {
                    Pads =
                    [
                        new PadDetail { BM = 1.3, LM = 1.0, HeightM = 0.45 }
                    ]
                }
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.Foundations!.Pads.Should().HaveCount(1);
        details.Foundations.Pads[0].BM.Should().Be(1.3);
    }

    [Fact]
    public void Aggregate_mapsTimberGroupsFromRoofDrawing()
    {
        List<FloorPlanDrawing> drawings =
        [
            new FloorPlanDrawing
            {
                Classification = new DrawingClassification { DrawingType = "rzut_wiezby_dachowej", SheetNumber = "K-04" },
                Roof = new RoofSection
                {
                    TimberGroups =
                    [
                        new TimberGroup
                        {
                            Name = "Krokwie",
                            Section = "8x16",
                            Rows = [new TimberGroupRow { Count = 12, LengthM = 4.5, RowSumMb = 54 }],
                            GroupVolumeM3 = 0.86
                        }
                    ],
                    TotalVolumeM3 = 0.86
                }
            }
        ];

        ProjectTechnicalDocumentationDetails details = TechnicalDocumentationDetailsAggregator.Aggregate(drawings);

        details.TimberStructure!.Groups.Should().HaveCount(1);
        details.TimberStructure.Groups[0].Name.Should().Be("Krokwie");
        details.TimberStructure.Groups[0].Section.Should().Be("8x16");
    }
}
