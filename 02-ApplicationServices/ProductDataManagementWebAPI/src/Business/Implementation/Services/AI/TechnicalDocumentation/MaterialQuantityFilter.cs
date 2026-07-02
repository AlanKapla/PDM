using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class MaterialQuantityFilter
{
    private const double MinimumQuantity = 0.001;

    public static bool HasPositiveQuantity(double quantity)
    {
        return quantity > MinimumQuantity;
    }

    public static List<MaterialQuantity> Filter(IEnumerable<MaterialQuantity> items)
    {
        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.MaterialType))
            .Where(item => HasPositiveQuantity(item.Quantity))
            .ToList();
    }

    public static MaterialSchedule PruneZeroQuantities(MaterialSchedule schedule)
    {
        schedule.Summary = schedule.Summary
            .Where(item => !string.IsNullOrWhiteSpace(item.MaterialType))
            .Where(item => HasPositiveQuantity(item.GrossQuantity))
            .ToList();

        schedule.Masonry = PruneMaterialItems(schedule.Masonry);
        schedule.Insulation = PruneMaterialItems(schedule.Insulation);
        schedule.Concrete = PruneMaterialItems(schedule.Concrete);
        schedule.Steel = PruneMaterialItems(schedule.Steel);
        schedule.Timber = PruneMaterialItems(schedule.Timber);
        schedule.Roofing = PruneMaterialItems(schedule.Roofing);
        schedule.Finishes = PruneMaterialItems(schedule.Finishes);

        schedule.Foundations.Concrete = PruneMaterialItems(schedule.Foundations.Concrete);
        schedule.Foundations.Steel = PruneMaterialItems(schedule.Foundations.Steel);
        schedule.Foundations.Blocks = PruneMaterialItems(schedule.Foundations.Blocks);

        schedule.Walls.Masonry = PruneMaterialItems(schedule.Walls.Masonry);
        schedule.Walls.Mortar = PruneMaterialItems(schedule.Walls.Mortar);
        schedule.Walls.Insulation = PruneMaterialItems(schedule.Walls.Insulation);

        schedule.Ceilings.Concrete = PruneMaterialItems(schedule.Ceilings.Concrete);
        schedule.Ceilings.Steel = PruneMaterialItems(schedule.Ceilings.Steel);

        schedule.Columns.Concrete = PruneMaterialItems(schedule.Columns.Concrete);
        schedule.Columns.Steel = PruneMaterialItems(schedule.Columns.Steel);

        schedule.Roof.Covering = PruneMaterialItems(schedule.Roof.Covering);
        schedule.Roof.Timber = PruneMaterialItems(schedule.Roof.Timber);
        schedule.Roof.Insulation = PruneMaterialItems(schedule.Roof.Insulation);

        schedule.Openings = schedule.Openings
            .Where(opening => opening.Count > 0)
            .ToList();

        return MaterialUnitNormalizer.NormalizeSchedule(schedule);
    }

    private static List<MaterialItem> PruneMaterialItems(List<MaterialItem> items)
    {
        return items
            .Where(item => HasPositiveQuantity(item.GrossQuantity) || HasPositiveQuantity(item.NetQuantity))
            .ToList();
    }
}
