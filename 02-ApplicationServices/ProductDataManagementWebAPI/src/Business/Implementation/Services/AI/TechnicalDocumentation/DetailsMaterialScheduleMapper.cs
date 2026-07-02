using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class DetailsMaterialScheduleMapper
{
    public static DetailsMaterialSchedule Map(
        MaterialSchedule schedule,
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings)
    {
        DetailsMaterialSchedule details = new()
        {
            CalculatedAt = schedule.CalculatedAt,
            Groups = new DetailsMaterialScheduleGroups
            {
                Foundations = new DetailsMaterialScheduleFoundationGroup
                {
                    Concrete = MapItems(schedule.Foundations.Concrete, "m3"),
                    Steel = MapItems(schedule.Foundations.Steel, "kg"),
                    Masonry = MapItems(schedule.Foundations.Blocks, "m2"),
                    Insulation = MapItems(schedule.Insulation, "m2")
                },
                Slabs = new DetailsMaterialScheduleSlabGroup
                {
                    Concrete = MapItems(schedule.Ceilings.Concrete, "m3"),
                    Steel = MapItems(schedule.Ceilings.Steel, "kg")
                },
                Roof = new DetailsMaterialScheduleRoofGroup
                {
                    Timber = MapItems(schedule.Roof.Timber, "m3"),
                    Covering = MapItems(schedule.Roof.Covering, "m2")
                },
                Site = new DetailsMaterialScheduleSiteGroup
                {
                    PlotAreaM2 = model.Site.PlotAreaM2,
                    BuildingFootprintM2 = model.Site.BuildingFootprintM2,
                    CubatureM3 = model.Site.BuildingVolumeM3,
                    SourceDrawing = FindSiteDrawing(drawings)
                }
            },
            Totals = BuildTotals(schedule)
        };

        return details;
    }

    private static List<DetailsMaterialScheduleItem> MapItems(List<MaterialItem> items, string defaultUnit)
    {
        return items
            .Where(item => item.GrossQuantity > 0 || item.NetQuantity > 0)
            .Select(item => new DetailsMaterialScheduleItem
            {
                Element = string.IsNullOrWhiteSpace(item.Specification) ? item.Element : item.Specification,
                NetM3 = defaultUnit == "m3" ? item.NetQuantity : null,
                GrossM3 = defaultUnit == "m3" ? item.GrossQuantity : null,
                NetM2 = defaultUnit == "m2" ? item.NetQuantity : null,
                GrossM2 = defaultUnit == "m2" ? item.GrossQuantity : null,
                NetKg = defaultUnit == "kg" ? item.NetQuantity : null,
                GrossKg = defaultUnit == "kg" ? item.GrossQuantity : null,
                Unit = item.Unit,
                WastePercent = item.WastePercent,
                SourceType = item.SourceType,
                SourceDrawing = item.SourceDrawings.FirstOrDefault()
            })
            .ToList();
    }

    private static DetailsMaterialScheduleTotals BuildTotals(MaterialSchedule schedule)
    {
        double concrete = SumByUnit(schedule, "m3");
        double steel = schedule.Ceilings.Steel.Sum(item => item.GrossQuantity)
            + schedule.Foundations.Steel.Sum(item => item.GrossQuantity);
        double timber = schedule.Roof.Timber.Sum(item => item.GrossQuantity);
        double insulation = schedule.Insulation.Sum(item => item.GrossQuantity)
            + schedule.Walls.Insulation.Sum(item => item.GrossQuantity);

        return new DetailsMaterialScheduleTotals
        {
            ConcreteM3 = concrete > 0 ? Math.Round(concrete, 2) : null,
            SteelKg = steel > 0 ? Math.Round(steel, 2) : null,
            TimberM3 = timber > 0 ? Math.Round(timber, 2) : null,
            InsulationM2 = insulation > 0 ? Math.Round(insulation, 2) : null
        };
    }

    private static double SumByUnit(MaterialSchedule schedule, string unit)
    {
        IEnumerable<MaterialItem> allItems = schedule.Foundations.Concrete
            .Concat(schedule.Ceilings.Concrete)
            .Concat(schedule.Concrete);

        return allItems
            .Where(item => string.Equals(item.Unit, unit, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.GrossQuantity);
    }

    private static string? FindSiteDrawing(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        return drawings
            .FirstOrDefault(drawing =>
                drawing.Classification.DrawingType.Contains("zagospodarowanie", StringComparison.OrdinalIgnoreCase))
            ?.Classification.SheetNumber;
    }
}
