using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class ComparatorAgentServiceTests
{
    [Fact]
    public async Task CompareAsync_usesDeterministicMerge_withoutVisionLlm()
    {
        // Arrange
        ComparatorAgentService comparator = new(NullLogger<ComparatorAgentService>.Instance);

        FloorPlanDrawing resultA = new()
        {
            Rooms = [new Room { Name = "Salon", AreaM2 = 21.3 }],
            Openings = [new Opening { Type = "okno", Count = 2 }]
        };

        FloorPlanDrawing resultB = new()
        {
            Rooms = [new Room { Name = "Salon", AreaM2 = 21.0 }],
            Openings = [new Opening { Type = "okno", Count = 2 }]
        };

        DrawingClassification classification = new() { DrawingType = "rzut parteru" };

        // Act
        FloorPlanDrawing merged = await comparator.CompareAsync(
            [1, 2, 3],
            "image/jpeg",
            resultA,
            resultB,
            classification,
            CancellationToken.None);

        // Assert
        merged.Rooms.Should().HaveCount(1);
        merged.ValidationReport.Should().NotBeNull();
        merged.Classification.Should().BeSameAs(classification);
    }

    [Fact]
    public async Task CompareAsync_flagsMassSumConflict_whenTotalsDiverge()
    {
        // Arrange
        ComparatorAgentService comparator = new(NullLogger<ComparatorAgentService>.Instance);

        FloorPlanDrawing resultA = new()
        {
            Floors = new FloorSection
            {
                Bars = [new RebarBar { MassKg = 500 }],
                TotalMassKg = 1000
            }
        };

        FloorPlanDrawing resultB = new()
        {
            Floors = new FloorSection
            {
                Bars = [new RebarBar { MassKg = 900 }],
                TotalMassKg = 900
            }
        };

        DrawingClassification classification = new() { DrawingType = "zbrojenie_stropu_dolne" };

        // Act
        FloorPlanDrawing merged = await comparator.CompareAsync(
            [1, 2, 3],
            "image/jpeg",
            resultA,
            resultB,
            classification,
            CancellationToken.None);

        // Assert
        merged.ValidationReport.Should().NotBeNull();
        merged.ValidationReport!.Disagreements.Should().NotBeEmpty();
    }
}
