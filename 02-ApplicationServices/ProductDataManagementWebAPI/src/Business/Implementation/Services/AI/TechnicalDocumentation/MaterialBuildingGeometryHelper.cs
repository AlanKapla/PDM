using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal sealed class BuildingGeometry
{
    public double FootprintM2 { get; init; }
    public double PerimeterM { get; init; }
    public double WallHeightM { get; init; }
    public double FoundationWallHeightM { get; init; }
    public double SlabAreaM2 { get; init; }
    public double RoofAreaM2 { get; init; }
    public double RoofPitchDegrees { get; init; }
    public double AtticUsableAreaM2 { get; init; }
    public double ExternalWallThicknessM { get; init; }
}

internal static class MaterialBuildingGeometryHelper
{
    private const double DefaultWallHeightM = 2.7;
    private const double DefaultFoundationWallHeightM = 0.6;
    private const double DefaultExternalWallThicknessM = 0.24;
    private const double DefaultFootingWidthM = 0.6;
    private const double DefaultFootingHeightM = 0.4;

    public static BuildingGeometry Resolve(
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyDictionary<string, object> sharedState)
    {
        double footprintM2 = ResolveFootprintM2(model, drawings);
        double perimeterM = EstimatePerimeterM(footprintM2);
        SectionDrawingData? section = ResolvePrimarySection(drawings);
        double wallHeightM = ResolveWallHeightM(section);
        double foundationWallHeightM = ResolveFoundationWallHeightM(section);
        double slabAreaM2 = ResolveSlabAreaM2(model, drawings);
        double roofAreaM2 = ResolveRoofAreaM2(model, drawings, sharedState);
        double pitchDegrees = ResolveRoofPitch(model, drawings, sharedState);
        double atticAreaM2 = ResolveAtticUsableAreaM2(model);
        double wallThicknessM = ResolveExternalWallThicknessM(model);

        return new BuildingGeometry
        {
            FootprintM2 = footprintM2,
            PerimeterM = perimeterM,
            WallHeightM = wallHeightM,
            FoundationWallHeightM = foundationWallHeightM,
            SlabAreaM2 = slabAreaM2,
            RoofAreaM2 = roofAreaM2,
            RoofPitchDegrees = pitchDegrees,
            AtticUsableAreaM2 = atticAreaM2,
            ExternalWallThicknessM = wallThicknessM
        };
    }

    public static double EstimatePerimeterM(double footprintM2)
    {
        if (footprintM2 <= 0)
        {
            return 0;
        }

        return Math.Round(4 * Math.Sqrt(footprintM2 * 1.25), 1);
    }

    private static double ResolveFootprintM2(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        if (model.Site.BuildingFootprintM2 is > 0)
        {
            return model.Site.BuildingFootprintM2.Value;
        }

        double? fromDrawing = drawings
            .Select(drawing => drawing.Site?.BuildingFootprintM2 ?? drawing.TotalAreaM2)
            .FirstOrDefault(area => area is > 0);

        if (fromDrawing is > 0)
        {
            return fromDrawing.Value;
        }

        ProjectModelFloor? groundFloor = model.Floors
            .FirstOrDefault(floor => floor.Level.Contains("parter", StringComparison.OrdinalIgnoreCase));

        if (groundFloor is not null)
        {
            double total = groundFloor.TotalAreaM2
                ?? groundFloor.Rooms.Sum(room => room.AreaM2 ?? 0);

            if (total > 0)
            {
                return Math.Round(total, 1);
            }
        }

        return 0;
    }

    private static SectionDrawingData? ResolvePrimarySection(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        return drawings
            .Select(drawing => drawing.Section)
            .FirstOrDefault(section => section?.Levels is not null);
    }

    private static double ResolveWallHeightM(SectionDrawingData? section)
    {
        if (section?.Levels?.CeilingM is > 0)
        {
            double ground = section.Levels.GroundFloorM ?? 0;
            return Math.Round(section.Levels.CeilingM.Value - ground, 2);
        }

        return DefaultWallHeightM;
    }

    private static double ResolveFoundationWallHeightM(SectionDrawingData? section)
    {
        if (section?.Levels?.FoundationBottomM is not null && section.Levels.GroundFloorM is not null)
        {
            double height = section.Levels.GroundFloorM.Value - section.Levels.FoundationBottomM.Value;
            if (height > 0 && height < 3)
            {
                return Math.Round(Math.Min(height, 1.0), 2);
            }
        }

        return DefaultFoundationWallHeightM;
    }

    private static double ResolveSlabAreaM2(ProjectModel model, IReadOnlyList<FloorPlanDrawing> drawings)
    {
        ProjectModelFloor? groundFloor = model.Floors
            .FirstOrDefault(floor => floor.Level.Contains("parter", StringComparison.OrdinalIgnoreCase));

        if (groundFloor is null)
        {
            return 0;
        }

        double planArea = groundFloor.TotalAreaM2
            ?? groundFloor.Rooms.Sum(room => room.AreaM2 ?? 0);

        if (planArea <= 0)
        {
            return 0;
        }

        double garageArea = groundFloor.Rooms
            .Where(room => room.Name.Contains("garaż", StringComparison.OrdinalIgnoreCase))
            .Sum(room => room.AreaM2 ?? 0);

        if (garageArea <= 0)
        {
            garageArea = drawings
                .SelectMany(drawing => drawing.Rooms)
                .Where(room => room.Name.Contains("garaż", StringComparison.OrdinalIgnoreCase))
                .Sum(room => room.AreaM2);
        }

        return Math.Round(Math.Max(planArea - garageArea, 0), 1);
    }

    private static double ResolveRoofAreaM2(
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyDictionary<string, object> sharedState)
    {
        if (sharedState.TryGetValue("roof.areaM2", out object? sharedArea)
            && sharedArea is double area
            && area > 0)
        {
            return area;
        }

        if (model.Roof.AreaM2 is > 0)
        {
            return model.Roof.AreaM2.Value;
        }

        double? fromDrawing = drawings
            .Select(drawing => drawing.Roof?.AreaM2)
            .Where(roofArea => roofArea is > 0)
            .DefaultIfEmpty()
            .Max();

        return fromDrawing ?? 0;
    }

    private static double ResolveRoofPitch(
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyDictionary<string, object> sharedState)
    {
        if (sharedState.TryGetValue("roof.pitchDegrees", out object? sharedPitch)
            && sharedPitch is double pitch
            && pitch > 0)
        {
            return pitch;
        }

        if (model.Roof.PitchDegrees is > 0)
        {
            return model.Roof.PitchDegrees.Value;
        }

        return drawings
            .Select(drawing => drawing.Roof?.PitchDegrees)
            .FirstOrDefault(degrees => degrees is > 0) ?? 35;
    }

    private static double ResolveAtticUsableAreaM2(ProjectModel model)
    {
        ProjectModelFloor? attic = model.Floors
            .FirstOrDefault(floor => floor.Level.Contains("poddasze", StringComparison.OrdinalIgnoreCase));

        if (attic is null)
        {
            return 0;
        }

        return attic.TotalAreaM2
            ?? attic.Rooms.Sum(room => room.AreaM2 ?? 0);
    }

    private static double ResolveExternalWallThicknessM(ProjectModel model)
    {
        double? thicknessCm = model.Walls.External.ThicknessCm
            ?? model.Walls.External.Layers
                .Where(layer => layer.ThicknessCm is > 0)
                .Select(layer => layer.ThicknessCm)
                .FirstOrDefault();

        if (thicknessCm is > 0)
        {
            return Math.Round(thicknessCm.Value / 100.0, 3);
        }

        return DefaultExternalWallThicknessM;
    }

    public static double ResolveFootingLinearMeters(double perimeterM)
    {
        return Math.Round(perimeterM * 1.5, 1);
    }

    public static double ResolveFootingLinearMetersFromFootings(IEnumerable<FootingDetail> footings)
    {
        double total = 0;

        foreach (FootingDetail footing in footings)
        {
            if (footing.Segments.Count > 0)
            {
                total += footing.Segments.Sum(segment => segment.LengthM);
                continue;
            }

            if (footing.LengthM > 0)
            {
                total += footing.LengthM;
            }
        }

        return Math.Round(total, 1);
    }

    public static double DefaultFootingWidth => DefaultFootingWidthM;

    public static double DefaultFootingHeight => DefaultFootingHeightM;
}
