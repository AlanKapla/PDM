using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class ProjectModelMaterialScheduleBuilder
{
    public static MaterialSchedule Build(ProjectModel projectModel, string buildingType)
    {
        MaterialSchedule schedule = new()
        {
            CalculatedAt = DateTime.UtcNow,
        };

        if (projectModel.Slab is not null)
        {
            AppendSlabMaterials(schedule, projectModel.Slab);
        }

        foreach (ProjectModelCeiling ceiling in projectModel.Ceilings)
        {
            AppendCeilingMaterials(schedule, ceiling);
        }

        if (!string.IsNullOrWhiteSpace(projectModel.Foundations.Concrete))
        {
            schedule.Foundations.Concrete.Add(new MaterialItem
            {
                Element = $"Beton fundamentów — {projectModel.Foundations.Concrete}",
                SourceType = "read",
                Unit = "m3",
            });
        }

        if (projectModel.Roof.AreaM2 is > 0 && !string.IsNullOrWhiteSpace(projectModel.Roof.CoveringType))
        {
            schedule.Roof.Covering.Add(new MaterialItem
            {
                Element = projectModel.Roof.CoveringType,
                NetQuantity = projectModel.Roof.AreaM2.Value,
                GrossQuantity = projectModel.Roof.AreaM2.Value,
                Unit = "m2",
                SourceType = "read",
            });
        }

        if (projectModel.Roof.TotalTimberVolumeM3 is > 0)
        {
            schedule.Roof.Timber.Add(new MaterialItem
            {
                Element = "Drewno więźby dachowej — łącznie",
                NetQuantity = projectModel.Roof.TotalTimberVolumeM3.Value,
                GrossQuantity = Math.Round(projectModel.Roof.TotalTimberVolumeM3.Value * 1.1, 2),
                WastePercent = 10,
                Unit = "m3",
                SourceType = "read",
                Specification = projectModel.Roof.WoodClass,
            });
        }

        schedule.Summary = MaterialScheduleBuilder.BuildSummaryPublic(schedule);
        schedule.Warnings.Add(
            $"Harmonogram zbudowany z ProjectModel §8.1 ({buildingType}) — brak rysunków per-drawing w pipeline grupowym.");

        return MaterialQuantityFilter.PruneZeroQuantities(schedule);
    }

    private static void AppendSlabMaterials(MaterialSchedule schedule, ProjectModelSlab slab)
    {
        if (slab.SteelBottomKg is > 0)
        {
            schedule.Ceilings.Steel.Add(new MaterialItem
            {
                Element = slab.CoverageDescription ?? "Stal zbrojenia dolnego",
                NetQuantity = slab.SteelBottomKg.Value,
                GrossQuantity = slab.SteelBottomKg.Value,
                Unit = "kg",
                SourceType = "read",
                SourceDrawings = ["K-02"],
            });
        }

        if (slab.SteelTopKg is > 0)
        {
            schedule.Ceilings.Steel.Add(new MaterialItem
            {
                Element = "Stal zbrojenia górnego",
                NetQuantity = slab.SteelTopKg.Value,
                GrossQuantity = slab.SteelTopKg.Value,
                Unit = "kg",
                SourceType = "read",
                SourceDrawings = ["K-03"],
            });
        }
    }

    private static void AppendCeilingMaterials(MaterialSchedule schedule, ProjectModelCeiling ceiling)
    {
        if (ceiling.SteelBottomKg is > 0)
        {
            schedule.Ceilings.Steel.Add(new MaterialItem
            {
                Element = ceiling.CoverageDescription ?? "Stal zbrojenia dolnego",
                NetQuantity = ceiling.SteelBottomKg.Value,
                GrossQuantity = ceiling.SteelBottomKg.Value,
                Unit = "kg",
                SourceType = "read",
            });
        }

        if (ceiling.SteelTopKg is > 0)
        {
            schedule.Ceilings.Steel.Add(new MaterialItem
            {
                Element = "Stal zbrojenia górnego",
                NetQuantity = ceiling.SteelTopKg.Value,
                GrossQuantity = ceiling.SteelTopKg.Value,
                Unit = "kg",
                SourceType = "read",
            });
        }
    }
}
