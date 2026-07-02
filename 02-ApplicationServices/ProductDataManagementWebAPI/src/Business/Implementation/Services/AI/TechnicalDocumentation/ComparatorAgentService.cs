using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class ComparatorAgentService : IComparatorAgent
{
    private const double SumToleranceRatio = 0.01;

    private readonly ILogger<ComparatorAgentService> logger;

    public ComparatorAgentService(ILogger<ComparatorAgentService> logger)
    {
        this.logger = logger;
    }

    public Task<FloorPlanDrawing> CompareAsync(
        byte[] imageBytes,
        string mediaType,
        FloorPlanDrawing resultA,
        FloorPlanDrawing resultB,
        DrawingClassification classification,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Using deterministic comparator merge with sum verification (no vision LLM)");

        List<string> sumConflicts = DetectSumConflicts(resultA, resultB);
        foreach (string conflict in sumConflicts)
        {
            logger.LogWarning("Agent A/B sum conflict for {DrawingType}: {Conflict}", classification.DrawingType, conflict);
        }

        FloorPlanDrawing validated = FloorPlanDrawingMerger.Merge(resultA, resultB);
        validated.Classification = classification;
        return Task.FromResult(validated);
    }

    private static List<string> DetectSumConflicts(FloorPlanDrawing resultA, FloorPlanDrawing resultB)
    {
        List<string> conflicts = new();

        AddMassConflict(resultA.Floors, resultB.Floors, conflicts);
        AddVolumeConflict(resultA.Roof, resultB.Roof, conflicts);
        AddAreaConflict(resultA, resultB, conflicts);
        AddFootingSegmentConflict(resultA.Foundations, resultB.Foundations, conflicts);

        return conflicts;
    }

    private static void AddMassConflict(FloorSection? floorsA, FloorSection? floorsB, List<string> conflicts)
    {
        if (floorsA is null || floorsB is null)
        {
            return;
        }

        double sumA = floorsA.Bars.Sum(bar => bar.MassKg ?? 0);
        double sumB = floorsB.Bars.Sum(bar => bar.MassKg ?? 0);
        double? totalA = floorsA.TotalMassKg;
        double? totalB = floorsB.TotalMassKg;

        if (totalA is > 0 && sumA > 0 && !WithinTolerance(sumA, totalA.Value))
        {
            conflicts.Add($"Agent A bar mass sum {sumA:0.##} kg vs totalMassKg {totalA:0.##} kg");
        }

        if (totalB is > 0 && sumB > 0 && !WithinTolerance(sumB, totalB.Value))
        {
            conflicts.Add($"Agent B bar mass sum {sumB:0.##} kg vs totalMassKg {totalB:0.##} kg");
        }

        if (totalA is > 0 && totalB is > 0 && !WithinTolerance(totalA.Value, totalB.Value))
        {
            conflicts.Add($"totalMassKg A={totalA:0.##} kg vs B={totalB:0.##} kg");
        }
    }

    private static void AddVolumeConflict(RoofSection? roofA, RoofSection? roofB, List<string> conflicts)
    {
        if (roofA is null || roofB is null)
        {
            return;
        }

        double sumA = roofA.TimberGroups.Sum(group => group.GroupVolumeM3 ?? 0);
        double sumB = roofB.TimberGroups.Sum(group => group.GroupVolumeM3 ?? 0);

        if (roofA.TotalVolumeM3 is > 0 && sumA > 0 && !WithinTolerance(sumA, roofA.TotalVolumeM3.Value))
        {
            conflicts.Add($"Agent A timber volume sum {sumA:0.##} m3 vs totalVolumeM3 {roofA.TotalVolumeM3:0.##} m3");
        }

        if (roofB.TotalVolumeM3 is > 0 && sumB > 0 && !WithinTolerance(sumB, roofB.TotalVolumeM3.Value))
        {
            conflicts.Add($"Agent B timber volume sum {sumB:0.##} m3 vs totalVolumeM3 {roofB.TotalVolumeM3:0.##} m3");
        }

        if (roofA.TotalVolumeM3 is > 0 && roofB.TotalVolumeM3 is > 0
            && !WithinTolerance(roofA.TotalVolumeM3.Value, roofB.TotalVolumeM3.Value))
        {
            conflicts.Add($"totalVolumeM3 A={roofA.TotalVolumeM3:0.##} m3 vs B={roofB.TotalVolumeM3:0.##} m3");
        }
    }

    private static void AddAreaConflict(FloorPlanDrawing resultA, FloorPlanDrawing resultB, List<string> conflicts)
    {
        double roomsA = resultA.Rooms.Sum(room => room.AreaM2);
        double roomsB = resultB.Rooms.Sum(room => room.AreaM2);
        double? totalA = resultA.TotalAreaM2;
        double? totalB = resultB.TotalAreaM2;

        if (totalA is > 0 && roomsA > 0 && !WithinTolerance(roomsA, totalA.Value))
        {
            conflicts.Add($"Agent A room area sum {roomsA:0.##} m2 vs totalAreaM2 {totalA:0.##} m2");
        }

        if (totalB is > 0 && roomsB > 0 && !WithinTolerance(roomsB, totalB.Value))
        {
            conflicts.Add($"Agent B room area sum {roomsB:0.##} m2 vs totalAreaM2 {totalB:0.##} m2");
        }

        if (totalA is > 0 && totalB is > 0 && !WithinTolerance(totalA.Value, totalB.Value))
        {
            conflicts.Add($"totalAreaM2 A={totalA:0.##} m2 vs B={totalB:0.##} m2");
        }
    }

    private static void AddFootingSegmentConflict(
        FoundationSection? foundationsA,
        FoundationSection? foundationsB,
        List<string> conflicts)
    {
        if (foundationsA is null || foundationsB is null)
        {
            return;
        }

        foreach (FootingDetail footingA in foundationsA.Footings)
        {
            if (string.IsNullOrWhiteSpace(footingA.Symbol))
            {
                continue;
            }

            FootingDetail? footingB = foundationsB.Footings
                .FirstOrDefault(candidate => string.Equals(
                    candidate.Symbol,
                    footingA.Symbol,
                    StringComparison.OrdinalIgnoreCase));

            if (footingB is null)
            {
                continue;
            }

            double lengthA = ResolveFootingLengthM(footingA);
            double lengthB = ResolveFootingLengthM(footingB);

            if (lengthA > 0 && lengthB > 0 && !WithinTolerance(lengthA, lengthB))
            {
                conflicts.Add($"Footing {footingA.Symbol} segment length A={lengthA:0.##} m vs B={lengthB:0.##} m");
            }
        }
    }

    private static double ResolveFootingLengthM(FootingDetail footing)
    {
        if (footing.Segments.Count > 0)
        {
            return footing.Segments.Sum(segment => segment.LengthM);
        }

        return footing.LengthM;
    }

    private static bool WithinTolerance(double left, double right)
    {
        double max = Math.Max(Math.Abs(left), Math.Abs(right));
        if (max <= 0)
        {
            return true;
        }

        return Math.Abs(left - right) / max <= SumToleranceRatio;
    }
}
