using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationValidationReportBuilder
{
    private const double NumericTolerance = 0.05;

    public static ValidationReport Build(FloorPlanDrawing agentA, FloorPlanDrawing agentB)
    {
        int agreeing = 0;
        int disagreements = 0;
        int lowConfidence = 0;
        List<FieldDisagreement> fieldDisagreements = new();

        CompareCount(
            "rooms.count",
            agentA.Rooms.Count,
            agentB.Rooms.Count,
            ref agreeing,
            ref disagreements,
            ref lowConfidence,
            fieldDisagreements);

        CompareCount(
            "openings.count",
            agentA.Openings.Count,
            agentB.Openings.Count,
            ref agreeing,
            ref disagreements,
            ref lowConfidence,
            fieldDisagreements);

        double areaA = agentA.Rooms.Sum(GetRoomArea);
        double areaB = agentB.Rooms.Sum(GetRoomArea);
        CompareNumeric(
            "rooms.totalAreaM2",
            areaA,
            areaB,
            ref agreeing,
            ref disagreements,
            ref lowConfidence,
            fieldDisagreements);

        CompareTotalAreaM2(agentA, agentB, ref agreeing, ref disagreements, ref lowConfidence, fieldDisagreements);
        CompareFloorMassTotals(agentA.Floors, agentB.Floors, ref agreeing, ref disagreements, ref lowConfidence, fieldDisagreements);
        CompareRoofVolumeTotals(agentA.Roof, agentB.Roof, ref agreeing, ref disagreements, ref lowConfidence, fieldDisagreements);
        CompareFootingSegmentLengths(agentA.Foundations, agentB.Foundations, ref agreeing, ref disagreements, ref lowConfidence, fieldDisagreements);

        return new ValidationReport
        {
            TotalFields = agreeing + disagreements + lowConfidence,
            HighConfidence = agreeing,
            MediumConfidence = disagreements,
            LowConfidence = lowConfidence,
            Disagreements = fieldDisagreements
        };
    }

    private static void CompareCount(
        string fieldPath,
        int valueA,
        int valueB,
        ref int agreeing,
        ref int disagreements,
        ref int lowConfidence,
        List<FieldDisagreement> fieldDisagreements)
    {
        if (valueA == 0 && valueB == 0)
        {
            lowConfidence++;
            return;
        }

        if (valueA == valueB)
        {
            agreeing++;
            return;
        }

        disagreements++;
        fieldDisagreements.Add(new FieldDisagreement
        {
            FieldPath = fieldPath,
            ValueA = valueA.ToString(),
            ValueB = valueB.ToString(),
            Resolved = Math.Max(valueA, valueB).ToString(),
            ResolutionNote = "deterministic merge"
        });
    }

    private static void CompareNumeric(
        string fieldPath,
        double valueA,
        double valueB,
        ref int agreeing,
        ref int disagreements,
        ref int lowConfidence,
        List<FieldDisagreement> fieldDisagreements)
    {
        if (valueA == 0 && valueB == 0)
        {
            lowConfidence++;
            return;
        }

        double maxValue = Math.Max(Math.Abs(valueA), Math.Abs(valueB));
        if (maxValue > 0 && Math.Abs(valueA - valueB) / maxValue <= NumericTolerance)
        {
            agreeing++;
            return;
        }

        if (Math.Abs(valueA - valueB) <= NumericTolerance)
        {
            agreeing++;
            return;
        }

        disagreements++;
        fieldDisagreements.Add(new FieldDisagreement
        {
            FieldPath = fieldPath,
            ValueA = valueA.ToString("0.##"),
            ValueB = valueB.ToString("0.##"),
            Resolved = Math.Max(valueA, valueB).ToString("0.##"),
            ResolutionNote = "deterministic merge"
        });
    }

    private static double GetRoomArea(Room room)
    {
        if (room.AreaM2 > 0)
        {
            return room.AreaM2;
        }

        if (room.WidthM > 0 && room.LengthM > 0)
        {
            return Math.Round(room.WidthM * room.LengthM, 1);
        }

        return 0;
    }

    private static void CompareTotalAreaM2(
        FloorPlanDrawing agentA,
        FloorPlanDrawing agentB,
        ref int agreeing,
        ref int disagreements,
        ref int lowConfidence,
        List<FieldDisagreement> fieldDisagreements)
    {
        if (agentA.TotalAreaM2 is null && agentB.TotalAreaM2 is null)
        {
            return;
        }

        CompareNumeric(
            "totalAreaM2",
            agentA.TotalAreaM2 ?? 0,
            agentB.TotalAreaM2 ?? 0,
            ref agreeing,
            ref disagreements,
            ref lowConfidence,
            fieldDisagreements);
    }

    private static void CompareFloorMassTotals(
        FloorSection? floorsA,
        FloorSection? floorsB,
        ref int agreeing,
        ref int disagreements,
        ref int lowConfidence,
        List<FieldDisagreement> fieldDisagreements)
    {
        if (floorsA is null || floorsB is null)
        {
            return;
        }

        double sumA = floorsA.Bars.Sum(bar => bar.MassKg ?? 0);
        double sumB = floorsB.Bars.Sum(bar => bar.MassKg ?? 0);

        CompareNumeric(
            "floors.bars.totalMassKg",
            sumA,
            sumB,
            ref agreeing,
            ref disagreements,
            ref lowConfidence,
            fieldDisagreements);

        if (floorsA.TotalMassKg is not null || floorsB.TotalMassKg is not null)
        {
            CompareNumeric(
                "floors.totalMassKg",
                floorsA.TotalMassKg ?? 0,
                floorsB.TotalMassKg ?? 0,
                ref agreeing,
                ref disagreements,
                ref lowConfidence,
                fieldDisagreements);
        }
    }

    private static void CompareRoofVolumeTotals(
        RoofSection? roofA,
        RoofSection? roofB,
        ref int agreeing,
        ref int disagreements,
        ref int lowConfidence,
        List<FieldDisagreement> fieldDisagreements)
    {
        if (roofA is null || roofB is null)
        {
            return;
        }

        double sumA = roofA.TimberGroups.Sum(group => group.GroupVolumeM3 ?? 0);
        double sumB = roofB.TimberGroups.Sum(group => group.GroupVolumeM3 ?? 0);

        CompareNumeric(
            "roof.timberGroups.totalVolumeM3",
            sumA,
            sumB,
            ref agreeing,
            ref disagreements,
            ref lowConfidence,
            fieldDisagreements);

        if (roofA.TotalVolumeM3 is not null || roofB.TotalVolumeM3 is not null)
        {
            CompareNumeric(
                "roof.totalVolumeM3",
                roofA.TotalVolumeM3 ?? 0,
                roofB.TotalVolumeM3 ?? 0,
                ref agreeing,
                ref disagreements,
                ref lowConfidence,
                fieldDisagreements);
        }
    }

    private static void CompareFootingSegmentLengths(
        FoundationSection? foundationsA,
        FoundationSection? foundationsB,
        ref int agreeing,
        ref int disagreements,
        ref int lowConfidence,
        List<FieldDisagreement> fieldDisagreements)
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

            CompareNumeric(
                $"foundations.footings.{footingA.Symbol}.segmentLengthM",
                lengthA,
                lengthB,
                ref agreeing,
                ref disagreements,
                ref lowConfidence,
                fieldDisagreements);
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
}
