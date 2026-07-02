using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class MaterialScheduleMerger
{
    public static MaterialSchedule Merge(IReadOnlyList<MaterialSchedule> schedules)
    {
        MaterialSchedule merged = new()
        {
            CalculatedAt = DateTime.UtcNow
        };

        foreach (MaterialSchedule schedule in schedules)
        {
            AppendSchedule(merged, schedule);
        }

        DeduplicateInsulation(merged);
        PreferReadRoofTimberFromProjectModel(merged, schedules);

        merged.Summary = MaterialScheduleBuilder.BuildSummaryPublic(merged);
        return MaterialQuantityFilter.PruneZeroQuantities(merged);
    }

    public static MaterialSchedule Overlay(MaterialSchedule baseSchedule, MaterialSchedule overlay)
    {
        OverlayItems(baseSchedule.Masonry, overlay.Masonry);
        OverlayItems(baseSchedule.Insulation, overlay.Insulation);
        OverlayItems(baseSchedule.Concrete, overlay.Concrete);
        OverlayItems(baseSchedule.Steel, overlay.Steel);
        OverlayItems(baseSchedule.Timber, overlay.Timber);
        OverlayItems(baseSchedule.Roofing, overlay.Roofing);
        OverlayItems(baseSchedule.Finishes, overlay.Finishes);

        OverlayItems(baseSchedule.Foundations.Concrete, overlay.Foundations.Concrete);
        OverlayItems(baseSchedule.Foundations.Steel, overlay.Foundations.Steel);
        OverlayItems(baseSchedule.Foundations.Blocks, overlay.Foundations.Blocks);

        OverlayItems(baseSchedule.Walls.Masonry, overlay.Walls.Masonry);
        OverlayItems(baseSchedule.Walls.Mortar, overlay.Walls.Mortar);
        OverlayItems(baseSchedule.Walls.Insulation, overlay.Walls.Insulation);

        OverlayItems(baseSchedule.Ceilings.Concrete, overlay.Ceilings.Concrete);
        OverlayItems(baseSchedule.Ceilings.Steel, overlay.Ceilings.Steel);

        OverlayItems(baseSchedule.Columns.Concrete, overlay.Columns.Concrete);
        OverlayItems(baseSchedule.Columns.Steel, overlay.Columns.Steel);

        OverlayItems(baseSchedule.Roof.Covering, overlay.Roof.Covering);
        OverlayItems(baseSchedule.Roof.Timber, overlay.Roof.Timber);
        OverlayItems(baseSchedule.Roof.Insulation, overlay.Roof.Insulation);

        baseSchedule.DrawingsUsed = baseSchedule.DrawingsUsed
            .Concat(overlay.DrawingsUsed)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        baseSchedule.MissingDimensions = baseSchedule.MissingDimensions
            .Concat(overlay.MissingDimensions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        baseSchedule.Assumptions = baseSchedule.Assumptions
            .Concat(overlay.Assumptions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        baseSchedule.Warnings = baseSchedule.Warnings
            .Concat(overlay.Warnings)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        MaterialScheduleCategorySync.SyncFlatCategories(baseSchedule);
        return baseSchedule;
    }

    private static void AppendSchedule(MaterialSchedule target, MaterialSchedule source)
    {
        target.Masonry.AddRange(source.Masonry);
        target.Insulation.AddRange(source.Insulation);
        target.Concrete.AddRange(source.Concrete);
        target.Steel.AddRange(source.Steel);
        target.Timber.AddRange(source.Timber);
        target.Roofing.AddRange(source.Roofing);
        target.Finishes.AddRange(source.Finishes);
        target.Openings.AddRange(source.Openings);

        target.Foundations.Concrete.AddRange(source.Foundations.Concrete);
        target.Foundations.Steel.AddRange(source.Foundations.Steel);
        target.Foundations.Blocks.AddRange(source.Foundations.Blocks);

        target.Walls.Masonry.AddRange(source.Walls.Masonry);
        target.Walls.Mortar.AddRange(source.Walls.Mortar);
        target.Walls.Insulation.AddRange(source.Walls.Insulation);

        target.Ceilings.Concrete.AddRange(source.Ceilings.Concrete);
        target.Ceilings.Steel.AddRange(source.Ceilings.Steel);

        target.Columns.Concrete.AddRange(source.Columns.Concrete);
        target.Columns.Steel.AddRange(source.Columns.Steel);

        target.Roof.Covering.AddRange(source.Roof.Covering);
        target.Roof.Timber.AddRange(source.Roof.Timber);
        target.Roof.Insulation.AddRange(source.Roof.Insulation);

        target.DrawingsUsed.AddRange(source.DrawingsUsed);
        target.MissingDimensions.AddRange(source.MissingDimensions);
        target.Assumptions.AddRange(source.Assumptions);
        target.Warnings.AddRange(source.Warnings);
    }

    private static void OverlayItems(List<MaterialItem> target, List<MaterialItem> overlay)
    {
        foreach (MaterialItem item in overlay)
        {
            if (item.GrossQuantity <= 0 && item.NetQuantity <= 0)
            {
                continue;
            }

            int existingIndex = target.FindIndex(candidate =>
                string.Equals(candidate.Element, item.Element, StringComparison.OrdinalIgnoreCase)
                && string.Equals(candidate.Unit, item.Unit, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                target[existingIndex] = item;
            }
            else
            {
                target.Add(item);
            }
        }
    }

    private static void DeduplicateInsulation(MaterialSchedule schedule)
    {
        schedule.Insulation = DeduplicateMaterialItems(schedule.Insulation);
        schedule.Walls.Insulation = DeduplicateMaterialItems(schedule.Walls.Insulation);
        schedule.Roof.Insulation = DeduplicateMaterialItems(schedule.Roof.Insulation);
    }

    private static List<MaterialItem> DeduplicateMaterialItems(List<MaterialItem> items)
    {
        return items
            .GroupBy(item => NormalizeElementKey(item.Element), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(candidate => candidate.GrossQuantity).First())
            .ToList();
    }

    private static string NormalizeElementKey(string? element)
    {
        if (string.IsNullOrWhiteSpace(element))
        {
            return string.Empty;
        }

        return element.Trim().ToLowerInvariant();
    }

    private static void PreferReadRoofTimberFromProjectModel(
        MaterialSchedule merged,
        IReadOnlyList<MaterialSchedule> schedules)
    {
        MaterialItem? readTimber = schedules
            .SelectMany(schedule => schedule.Roof.Timber)
            .Where(item => string.Equals(item.SourceType, "read", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.GrossQuantity)
            .FirstOrDefault();

        if (readTimber is null || readTimber.GrossQuantity <= 0)
        {
            return;
        }

        if (merged.Roof.Timber.Count == 0)
        {
            merged.Roof.Timber.Add(readTimber);
            return;
        }

        MaterialItem existing = merged.Roof.Timber
            .OrderByDescending(item => item.GrossQuantity)
            .First();

        if (existing.GrossQuantity < readTimber.GrossQuantity * 0.9)
        {
            merged.Roof.Timber.Clear();
            merged.Roof.Timber.Add(readTimber);
        }
    }
}
