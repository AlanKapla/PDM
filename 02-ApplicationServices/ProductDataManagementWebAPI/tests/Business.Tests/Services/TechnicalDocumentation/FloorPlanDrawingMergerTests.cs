using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class FloorPlanDrawingMergerTests
{
    [Fact]
    public void Merge_prefersHigherRoomAreaAndMergesOpenings()
    {
        FloorPlanDrawing agentA = new()
        {
            Rooms =
            [
                new Room
                {
                    Name = "Salon",
                    AreaM2 = 20
                }
            ],
            Openings =
            [
                new Opening { Type = "okno", WidthCm = 120, HeightCm = 140, Count = 1 }
            ]
        };

        FloorPlanDrawing agentB = new()
        {
            Rooms =
            [
                new Room
                {
                    Name = "Salon",
                    AreaM2 = 21.3
                }
            ],
            Openings =
            [
                new Opening { Type = "okno", WidthCm = 120, HeightCm = 140, Count = 2 }
            ]
        };

        FloorPlanDrawing merged = FloorPlanDrawingMerger.Merge(agentA, agentB);

        merged.Rooms.Should().HaveCount(1);
        merged.Rooms[0].AreaM2.Should().Be(21.3);
        merged.Openings.Should().HaveCount(1);
        merged.Openings[0].Count.Should().Be(2);
        merged.ValidationReport.Should().NotBeNull();
    }
}
