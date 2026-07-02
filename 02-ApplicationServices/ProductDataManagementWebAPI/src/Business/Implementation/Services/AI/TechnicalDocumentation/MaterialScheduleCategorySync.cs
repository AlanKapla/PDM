using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class MaterialScheduleCategorySync
{
    public static void SyncFlatCategories(MaterialSchedule schedule)
    {
        schedule.Masonry = MergeItems(schedule.Foundations.Blocks, schedule.Walls.Masonry);
        schedule.Concrete = MergeItems(schedule.Foundations.Concrete, schedule.Ceilings.Concrete);
        schedule.Steel = MergeItems(schedule.Foundations.Steel, schedule.Ceilings.Steel, schedule.Columns.Steel);
        schedule.Timber = CloneItems(schedule.Roof.Timber);
        schedule.Roofing = CloneItems(schedule.Roof.Covering);
        schedule.Insulation = MergeItems(
            schedule.Walls.Insulation,
            schedule.Roof.Insulation,
            schedule.Insulation);
        schedule.Finishes = MergeItems(schedule.Finishes, schedule.Walls.Mortar);
    }

    private static List<MaterialItem> MergeItems(params IEnumerable<MaterialItem>[] sources)
    {
        List<MaterialItem> merged = new();

        foreach (IEnumerable<MaterialItem> source in sources)
        {
            foreach (MaterialItem item in source)
            {
                if (item.NetQuantity <= 0 && item.GrossQuantity <= 0)
                {
                    continue;
                }

                merged.Add(item);
            }
        }

        return merged;
    }

    private static List<MaterialItem> CloneItems(IReadOnlyList<MaterialItem> items)
    {
        return items.Select(CloneItem).ToList();
    }

    private static MaterialItem CloneItem(MaterialItem item)
    {
        return new MaterialItem
        {
            Element = item.Element,
            Specification = item.Specification,
            Calculation = item.Calculation,
            SourceDrawings = item.SourceDrawings.ToList(),
            NetQuantity = item.NetQuantity,
            WastePercent = item.WastePercent,
            GrossQuantity = item.GrossQuantity,
            Unit = item.Unit,
            SourceType = item.SourceType,
            MissingData = item.MissingData
        };
    }
}
