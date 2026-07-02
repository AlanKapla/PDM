using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class MaterialWasteApplier
{
    public static MaterialSchedule Apply(MaterialSchedule schedule)
    {
        ApplyToItems(schedule.Foundations.Concrete, 0.05);
        ApplyToItems(schedule.Foundations.Steel, 0.10);
        ApplyToItems(schedule.Foundations.Blocks, 0.05);
        ApplyToItems(schedule.Walls.Masonry, 0.05);
        ApplyToItems(schedule.Walls.Mortar, 0.10);
        ApplyToItems(schedule.Walls.Insulation, 0.10);
        ApplyToItems(schedule.Ceilings.Concrete, 0.05);
        ApplyToItems(schedule.Ceilings.Steel, 0.10);
        ApplyToItems(schedule.Roof.Covering, 0.15);
        ApplyToItems(schedule.Roof.Timber, 0.10);
        ApplyToItems(schedule.Roof.Insulation, 0.10);
        ApplyToItems(schedule.Masonry, 0.05);
        ApplyToItems(schedule.Concrete, 0.05);
        ApplyToItems(schedule.Steel, 0.10);
        ApplyToItems(schedule.Timber, 0.10);
        ApplyToItems(schedule.Roofing, 0.15);
        ApplyToItems(schedule.Insulation, 0.10);
        ApplyToItems(schedule.Finishes, 0.10);

        return schedule;
    }

    private static void ApplyToItems(List<MaterialItem> items, double wastePercent)
    {
        foreach (MaterialItem item in items)
        {
            item.WastePercent = wastePercent * 100;
            item.GrossQuantity = Math.Round(item.NetQuantity * (1 + wastePercent), 2);
        }
    }

    private static double ResolveWastePercent(string category, string materialType)
    {
        string text = $"{category} {materialType}".ToLowerInvariant();

        if (text.Contains("stal", StringComparison.Ordinal) || text.Contains("q188", StringComparison.Ordinal))
        {
            return 0.10;
        }

        if (text.Contains("tynk", StringComparison.Ordinal) || text.Contains("zaprawa", StringComparison.Ordinal))
        {
            return 0.10;
        }

        if (text.Contains("dach", StringComparison.Ordinal)
            || text.Contains("dachówk", StringComparison.Ordinal)
            || text.Contains("dachowk", StringComparison.Ordinal))
        {
            return 0.15;
        }

        if (text.Contains("drewn", StringComparison.Ordinal)
            || text.Contains("krokw", StringComparison.Ordinal)
            || text.Contains("murłat", StringComparison.Ordinal)
            || text.Contains("murlat", StringComparison.Ordinal))
        {
            return 0.10;
        }

        return 0.05;
    }
}
