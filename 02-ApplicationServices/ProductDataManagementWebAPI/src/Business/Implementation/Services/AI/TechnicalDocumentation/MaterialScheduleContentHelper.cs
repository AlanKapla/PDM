using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class MaterialScheduleContentHelper
{
    public static bool HasMeaningfulContent(MaterialSchedule schedule)
    {
        if (schedule.Summary.Count > 0 || schedule.Openings.Count > 0)
        {
            return true;
        }

        if (HasMaterialItems(schedule.Masonry)
            || HasMaterialItems(schedule.Insulation)
            || HasMaterialItems(schedule.Concrete)
            || HasMaterialItems(schedule.Steel)
            || HasMaterialItems(schedule.Timber)
            || HasMaterialItems(schedule.Roofing)
            || HasMaterialItems(schedule.Finishes))
        {
            return true;
        }

        if (schedule.Foundations.Concrete.Count > 0
            || schedule.Foundations.Steel.Count > 0
            || schedule.Foundations.Blocks.Count > 0)
        {
            return true;
        }

        if (schedule.Walls.Masonry.Count > 0
            || schedule.Walls.Mortar.Count > 0
            || schedule.Walls.Insulation.Count > 0)
        {
            return true;
        }

        if (schedule.Ceilings.Concrete.Count > 0 || schedule.Ceilings.Steel.Count > 0)
        {
            return true;
        }

        if (schedule.Columns.Concrete.Count > 0 || schedule.Columns.Steel.Count > 0)
        {
            return true;
        }

        if (schedule.Roof.Covering.Count > 0
            || schedule.Roof.Timber.Count > 0
            || schedule.Roof.Insulation.Count > 0)
        {
            return true;
        }

        return false;
    }

    private static bool HasMaterialItems(List<MaterialItem> items)
    {
        return items.Count > 0;
    }
}
