using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

internal static class TechnicalDocumentationSharedStatePropagator
{
    public static void Propagate(IReadOnlyList<FloorPlanDrawing> drawings, Dictionary<string, object> sharedState)
    {
        foreach (FloorPlanDrawing drawing in drawings)
        {
            string? sheetNumber = drawing.Classification.SheetNumber;

            PropagateSlabThickness(drawing, sharedState, sheetNumber);
            PropagateSteelMass(drawing, sharedState, sheetNumber);
            PropagateTimberVolume(drawing, sharedState, sheetNumber);
            PropagateRoofPitch(drawing, sharedState, sheetNumber);
            PropagateRoofArea(drawing, sharedState, sheetNumber);
        }
    }

    private static void PropagateSlabThickness(
        FloorPlanDrawing drawing,
        Dictionary<string, object> sharedState,
        string? sheetNumber)
    {
        double? thicknessCm = ResolveSlabThicknessCm(drawing);

        if (!thicknessCm.HasValue)
        {
            return;
        }

        sharedState["ceiling.thicknessCm"] = thicknessCm.Value;

        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            sharedState[$"sheet:{NormalizeSheet(sheetNumber)}:ceiling.thicknessCm"] = thicknessCm.Value;
        }
    }

    private static void PropagateSteelMass(
        FloorPlanDrawing drawing,
        Dictionary<string, object> sharedState,
        string? sheetNumber)
    {
        double? totalMassKg = drawing.Floors?.TotalMassKg;

        if (!totalMassKg.HasValue || totalMassKg.Value <= 0)
        {
            if (drawing.Floors?.Steel.Count > 0)
            {
                totalMassKg = drawing.Floors.Steel.Sum(item => item.Quantity);
            }
        }

        if (!totalMassKg.HasValue || totalMassKg.Value <= 0)
        {
            return;
        }

        string role = drawing.Classification.DrawingType.Contains("gorne", StringComparison.OrdinalIgnoreCase)
            || drawing.Classification.DrawingType.Contains("górne", StringComparison.OrdinalIgnoreCase)
            ? "top"
            : "bottom";

        sharedState[$"reinforcement.{role}MassKg"] = totalMassKg.Value;
        sharedState["reinforcement.totalMassKg"] = totalMassKg.Value;

        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            sharedState[$"sheet:{NormalizeSheet(sheetNumber)}:reinforcement.totalMassKg"] = totalMassKg.Value;
        }
    }

    private static void PropagateRoofArea(
        FloorPlanDrawing drawing,
        Dictionary<string, object> sharedState,
        string? sheetNumber)
    {
        double? areaM2 = drawing.Roof?.AreaM2;

        if (!areaM2.HasValue || areaM2.Value <= 0)
        {
            return;
        }

        if (!sharedState.TryGetValue("roof.areaM2", out object? existing)
            || existing is not double existingArea
            || areaM2.Value > existingArea)
        {
            sharedState["roof.areaM2"] = areaM2.Value;
        }

        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            sharedState[$"sheet:{NormalizeSheet(sheetNumber)}:roof.areaM2"] = areaM2.Value;
        }
    }

    private static void PropagateTimberVolume(
        FloorPlanDrawing drawing,
        Dictionary<string, object> sharedState,
        string? sheetNumber)
    {
        double? totalVolumeM3 = drawing.Roof?.TotalVolumeM3;

        if (!totalVolumeM3.HasValue || totalVolumeM3.Value <= 0)
        {
            totalVolumeM3 = drawing.Roof?.TimberGroups
                .Where(group => group.GroupVolumeM3.HasValue)
                .Sum(group => group.GroupVolumeM3!.Value);

            if (!totalVolumeM3.HasValue || totalVolumeM3.Value <= 0)
            {
                return;
            }
        }

        sharedState["timber.totalVolumeM3"] = totalVolumeM3.Value;

        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            sharedState[$"sheet:{NormalizeSheet(sheetNumber)}:timber.totalVolumeM3"] = totalVolumeM3.Value;
        }
    }

    private static void PropagateRoofPitch(
        FloorPlanDrawing drawing,
        Dictionary<string, object> sharedState,
        string? sheetNumber)
    {
        double? pitchDegrees = drawing.Roof?.PitchDegrees;

        if ((!pitchDegrees.HasValue || pitchDegrees.Value <= 0) && drawing.Section?.Levels?.RidgeM is not null)
        {
            pitchDegrees = drawing.Roof?.PitchDegrees;
        }

        if (!pitchDegrees.HasValue || pitchDegrees.Value <= 0)
        {
            return;
        }

        sharedState["roof.pitchDegrees"] = pitchDegrees.Value;

        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            sharedState[$"sheet:{NormalizeSheet(sheetNumber)}:roof.pitchDegrees"] = pitchDegrees.Value;
        }
    }

    private static double? ResolveSlabThicknessCm(FloorPlanDrawing drawing)
    {
        SlabDetail? slab = drawing.Floors?.Slabs.FirstOrDefault(item => item.ThicknessCm > 0);

        if (slab is not null)
        {
            return slab.ThicknessCm;
        }

        return null;
    }

    private static string NormalizeSheet(string sheetNumber)
    {
        return sheetNumber.Trim().TrimStart('0');
    }
}
