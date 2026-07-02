using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class MaterialScheduleDrawingEnricher
{
    private const double ConcreteWastePercent = 5;
    private const double SteelWastePercent = 10;
    private const double MasonryWastePercent = 5;
    private const double InsulationWastePercent = 10;
    private const double RoofCoveringWastePercent = 15;
    private const double TimberWastePercent = 10;
    private const double OpeningsDeductionRatio = 0.15;

    public static MaterialSchedule Enrich(
        MaterialSchedule schedule,
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyDictionary<string, object> sharedState)
    {
        BuildingGeometry geometry = MaterialBuildingGeometryHelper.Resolve(projectModel, drawings, sharedState);

        EnrichFoundations(schedule, projectModel, drawings, geometry);
        EnrichWalls(schedule, projectModel, drawings, geometry);
        EnrichCeilings(schedule, projectModel, drawings, sharedState, geometry);
        EnrichRoof(schedule, projectModel, drawings, geometry);
        EnrichFloorAndThermalInsulation(schedule, drawings, geometry);

        MaterialScheduleCategorySync.SyncFlatCategories(schedule);

        return schedule;
    }

    private static void EnrichFoundations(
        MaterialSchedule schedule,
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        BuildingGeometry geometry)
    {
        FoundationSection? foundations = drawings
            .Select(drawing => drawing.Foundations)
            .FirstOrDefault(section => section is not null);

        string concreteClass = foundations?.ConcreteClass
            ?? projectModel.Foundations.Concrete
            ?? "B25";
        string sheet = drawings
            .FirstOrDefault(drawing => drawing.Foundations is not null)
            ?.Classification.SheetNumber ?? "K-01";

        if (schedule.Foundations.Concrete.Count == 0)
        {
            AddFoundationConcreteFromGeometry(schedule, foundations, geometry, concreteClass, sheet);
        }

        if (schedule.Foundations.Blocks.Count == 0 && geometry.PerimeterM > 0)
        {
            double blocksAreaM2 = Math.Round(geometry.PerimeterM * geometry.FoundationWallHeightM, 1);
            schedule.Foundations.Blocks.Add(CreateCalculatedMaterialItem(
                "Bloczki betonowe fundamentowe",
                blocksAreaM2,
                "m2",
                MasonryWastePercent,
                $"obwód {geometry.PerimeterM:F1} m × wys. {geometry.FoundationWallHeightM:F1} m",
                "K-01, A-05",
                "calculated"));
        }

        if (schedule.Foundations.Steel.Count == 0 && foundations?.Steel.Count > 0)
        {
            foreach (MaterialQuantity steel in foundations.Steel)
            {
                schedule.Foundations.Steel.Add(CreateReadMaterialItem(
                    steel.MaterialType,
                    steel.Quantity,
                    "kg",
                    SteelWastePercent,
                    $"Odczyt z {sheet}",
                    sheet));
            }
        }
        else if (schedule.Foundations.Steel.Count == 0 && geometry.FootprintM2 > 0)
        {
            double estimatedKg = Math.Round(geometry.FootprintM2 * 4.5, 0);
            schedule.Foundations.Steel.Add(CreateCalculatedMaterialItem(
                "Stal BST500S — zbrojenie ław i stóp",
                estimatedKg,
                "kg",
                SteelWastePercent,
                $"szacunek {geometry.FootprintM2:F0} m² × 4,5 kg/m²",
                sheet,
                "estimated"));
        }
    }

    private static void AddFoundationConcreteFromGeometry(
        MaterialSchedule schedule,
        FoundationSection? foundations,
        BuildingGeometry geometry,
        string concreteClass,
        string sheet)
    {
        double padVolumeM3 = 0;

        if (foundations is not null)
        {
            padVolumeM3 = foundations.Footings
                .Where(footing => footing.WidthM > 0 && footing.HeightM > 0)
                .Sum(footing =>
                {
                    double lengthM = ResolveFootingLengthM(footing);
                    return lengthM > 0 ? lengthM * footing.WidthM * footing.HeightM : 0;
                });

            padVolumeM3 += foundations.Pads
                .Where(pad => pad.BM > 0 && pad.LM > 0 && pad.HeightM > 0)
                .Sum(pad => pad.BM * pad.LM * pad.HeightM);
        }

        double footingLinearM = foundations is not null && foundations.Footings.Count > 0
            ? MaterialBuildingGeometryHelper.ResolveFootingLinearMetersFromFootings(foundations.Footings)
            : MaterialBuildingGeometryHelper.ResolveFootingLinearMeters(geometry.PerimeterM);

        double defaultFootingWidth = foundations?.Footings.FirstOrDefault(footing => footing.WidthM > 0)?.WidthM
            ?? MaterialBuildingGeometryHelper.DefaultFootingWidth;
        double defaultFootingHeight = foundations?.Footings.FirstOrDefault(footing => footing.HeightM > 0)?.HeightM
            ?? MaterialBuildingGeometryHelper.DefaultFootingHeight;

        double stripVolumeM3 = footingLinearM * defaultFootingWidth * defaultFootingHeight;

        double leanConcreteM3 = geometry.FootprintM2 > 0
            ? Math.Round(geometry.FootprintM2 * 0.1, 1)
            : 0;

        double totalNetM3 = Math.Round(stripVolumeM3 + padVolumeM3, 2);

        if (totalNetM3 > 0)
        {
            schedule.Foundations.Concrete.Add(CreateCalculatedMaterialItem(
                $"Beton {concreteClass} — ławy fundamentowe",
                totalNetM3,
                "m3",
                ConcreteWastePercent,
                $"ławy: {footingLinearM:F1} mb × {defaultFootingWidth:F2} × {defaultFootingHeight:F2} m"
                    + (padVolumeM3 > 0 ? $" + stopy: {padVolumeM3:F2} m³" : string.Empty),
                sheet,
                padVolumeM3 > 0 ? "read" : "calculated"));
        }

        if (leanConcreteM3 > 0 && !schedule.Foundations.Concrete.Any(item => item.Element.Contains("chudy", StringComparison.OrdinalIgnoreCase)))
        {
            schedule.Foundations.Concrete.Add(CreateCalculatedMaterialItem(
                "Chudy beton — podkład pod posadzkę",
                leanConcreteM3,
                "m3",
                ConcreteWastePercent,
                $"pow. zabudowy {geometry.FootprintM2:F0} m² × 10 cm",
                "A-05",
                "calculated"));
        }
    }

    private static void EnrichWalls(
        MaterialSchedule schedule,
        ProjectModel model,
        IReadOnlyList<FloorPlanDrawing> drawings,
        BuildingGeometry geometry)
    {
        if (geometry.PerimeterM <= 0 || geometry.WallHeightM <= 0)
        {
            return;
        }

        double grossExternalAreaM2 = geometry.PerimeterM * geometry.WallHeightM;
        double netExternalAreaM2 = Math.Round(grossExternalAreaM2 * (1 - OpeningsDeductionRatio), 1);
        double masonryVolumeM3 = Math.Round(netExternalAreaM2 * geometry.ExternalWallThicknessM, 2);

        string masonryMaterial = model.Walls.External.Layers
            .Select(layer => layer.Material)
            .FirstOrDefault(material => !string.IsNullOrWhiteSpace(material))
            ?? "beton komórkowy";

        if (schedule.Walls.Masonry.Count == 0 && masonryVolumeM3 > 0)
        {
            schedule.Walls.Masonry.Add(CreateCalculatedMaterialItem(
                $"{masonryMaterial} — ściany zewnętrzne",
                masonryVolumeM3,
                "m3",
                MasonryWastePercent,
                $"pow. netto {netExternalAreaM2:F1} m² × gr. {geometry.ExternalWallThicknessM * 100:F0} cm",
                "A-02, A-05",
                "calculated"));
        }

        ProjectModelWallLayer? modelInsulationLayer = model.Walls.External.Layers
            .FirstOrDefault(layer => TechnicalDocumentationDomainRules.IsThermalInsulationMaterial(layer.Material));

        WallLayer? drawingInsulationLayer = modelInsulationLayer is null
            ? drawings
                .SelectMany(drawing => drawing.Walls)
                .SelectMany(wall => wall.Layers)
                .FirstOrDefault(layer => TechnicalDocumentationDomainRules.IsThermalInsulationMaterial(layer.Material))
            : null;

        if (schedule.Walls.Insulation.Count == 0 && netExternalAreaM2 > 0)
        {
            string insulationName = modelInsulationLayer?.Material
                ?? drawingInsulationLayer?.Material
                ?? "styropian EPS 100";
            double thicknessCm = modelInsulationLayer?.ThicknessCm > 0
                ? modelInsulationLayer.ThicknessCm!.Value
                : drawingInsulationLayer?.ThicknessCm > 0
                    ? drawingInsulationLayer.ThicknessCm
                    : 10;

            schedule.Walls.Insulation.Add(CreateCalculatedMaterialItem(
                $"{insulationName} — ocieplenie ścian ETICS",
                netExternalAreaM2,
                "m2",
                InsulationWastePercent,
                $"pow. ścian zewn. netto {netExternalAreaM2:F1} m², gr. {thicknessCm:F0} cm",
                "A-05, A-06",
                "calculated"));
        }

        if (schedule.Walls.Mortar.Count == 0 && netExternalAreaM2 > 0)
        {
            schedule.Walls.Mortar.Add(CreateCalculatedMaterialItem(
                "Tynk elewacyjny cienkowarstwowy",
                netExternalAreaM2,
                "m2",
                InsulationWastePercent,
                $"pow. elewacji netto {netExternalAreaM2:F1} m²",
                "A-07..A-10",
                "estimated"));
        }
    }

    private static void EnrichCeilings(
        MaterialSchedule schedule,
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyDictionary<string, object> sharedState,
        BuildingGeometry geometry)
    {
        if (schedule.Ceilings.Steel.Count == 0)
        {
            foreach (FloorPlanDrawing drawing in SelectSlabReinforcementDrawings(drawings))
            {
                if (drawing.Floors is null)
                {
                    continue;
                }

                double steelKg = ResolveSteelMassKg(drawing.Floors);
                if (steelKg <= 0)
                {
                    continue;
                }

                bool isTop = drawing.Classification.DrawingType.Contains("gorne", StringComparison.OrdinalIgnoreCase)
                    || drawing.Classification.DrawingType.Contains("górne", StringComparison.OrdinalIgnoreCase);
                string sheet = drawing.Classification.SheetNumber ?? drawing.Source.FileName;
                string label = isTop
                    ? $"Stal zbrojenia górnego ({sheet})"
                    : $"Stal zbrojenia dolnego ({sheet})";

                schedule.Ceilings.Steel.Add(CreateReadMaterialItem(
                    label,
                    steelKg,
                    "kg",
                    SteelWastePercent,
                    $"Odczyt z tabeli zbrojenia {sheet}",
                    sheet));
            }
        }

        if (schedule.Ceilings.Concrete.Count == 0 && geometry.SlabAreaM2 > 0)
        {
            double? thicknessCm = ResolveSlabThicknessCm(drawings, sharedState) ?? 18;
            double netM3 = Math.Round(geometry.SlabAreaM2 * thicknessCm.Value / 100.0, 2);
            string concreteClass = ResolveSlabConcreteClass(drawings) ?? "C20/25 (B25)";

            schedule.Ceilings.Concrete.Add(CreateCalculatedMaterialItem(
                $"Beton {concreteClass} — strop żelbetowy",
                netM3,
                "m3",
                ConcreteWastePercent,
                $"pow. stropu {geometry.SlabAreaM2:F1} m² × gr. {thicknessCm:F0} cm",
                ResolveSlabSourceSheets(drawings),
                "calculated"));
        }
    }

    private static void EnrichRoof(
        MaterialSchedule schedule,
        ProjectModel projectModel,
        IReadOnlyList<FloorPlanDrawing> drawings,
        BuildingGeometry geometry)
    {
        double roofAreaM2 = geometry.RoofAreaM2;
        if (roofAreaM2 <= 0)
        {
            return;
        }

        if (schedule.Roof.Covering.Count == 0)
        {
            string covering = projectModel.Roof.CoveringType
                ?? drawings.Select(d => d.Roof?.CoveringType).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                ?? "dachówka";

            schedule.Roof.Covering.Add(CreateCalculatedMaterialItem(
                covering,
                roofAreaM2,
                "m2",
                RoofCoveringWastePercent,
                $"Powierzchnia dachu {roofAreaM2:F0} m²",
                "A-04",
                "read"));
        }
        else
        {
            MaterialItem existing = schedule.Roof.Covering[0];
            if (existing.NetQuantity <= 0)
            {
                existing.NetQuantity = roofAreaM2;
                existing.GrossQuantity = Math.Round(roofAreaM2 * (1 + RoofCoveringWastePercent / 100.0), 2);
                existing.Calculation = $"Powierzchnia dachu {roofAreaM2:F0} m²";
            }
        }

        if (schedule.Roof.Timber.Count == 0 && projectModel.Roof.TimberGroups.Count > 0)
        {
            foreach (ProjectModelTimberGroup group in projectModel.Roof.TimberGroups)
            {
                double volume = group.VolumeM3 ?? 0;
                if (volume <= 0 && group.LengthM is > 0 && !string.IsNullOrWhiteSpace(group.Section))
                {
                    volume = MaterialUnitNormalizer.ResolveTimberVolumeM3(new TimberElement
                    {
                        Element = group.Element,
                        Section = group.Section,
                        Count = group.Count ?? 1,
                        LengthM = group.LengthM.Value
                    });
                }

                if (volume <= 0)
                {
                    continue;
                }

                schedule.Roof.Timber.Add(CreateReadMaterialItem(
                    $"{group.Element} {group.Section}",
                    volume,
                    "m3",
                    TimberWastePercent,
                    "Odczyt z tabeli drewna K-04",
                    "K-04"));
            }
        }

        if (schedule.Roof.Insulation.Count == 0)
        {
            double atticArea = geometry.AtticUsableAreaM2 > 0 ? geometry.AtticUsableAreaM2 : geometry.SlabAreaM2;
            double roofInsulationArea = Math.Round(roofAreaM2 * 0.76, 1);

            schedule.Roof.Insulation.Add(CreateCalculatedMaterialItem(
                "Wełna mineralna — między krokwiami min. 18 cm",
                roofInsulationArea,
                "m2",
                InsulationWastePercent,
                $"pow. połaci ~{roofInsulationArea:F0} m² (nachylenie {geometry.RoofPitchDegrees:F0}°)",
                "A-05",
                "calculated"));

            if (atticArea > 0)
            {
                schedule.Roof.Insulation.Add(CreateCalculatedMaterialItem(
                    "Płyta G-K — sufit poddasza",
                    atticArea,
                    "m2",
                    InsulationWastePercent,
                    $"pow. użytkowa poddasza {atticArea:F1} m²",
                    "A-03, A-05",
                    "calculated"));
            }
        }
    }

    private static void EnrichFloorAndThermalInsulation(
        MaterialSchedule schedule,
        IReadOnlyList<FloorPlanDrawing> drawings,
        BuildingGeometry geometry)
    {
        SectionDrawingData? section = drawings
            .Select(drawing => drawing.Section)
            .FirstOrDefault(item => item?.FloorZones.Count > 0);

        if (section is null || geometry.SlabAreaM2 <= 0)
        {
            return;
        }

        SectionZone? zoneA = section.FloorZones
            .FirstOrDefault(zone => zone.Zone.Equals("A", StringComparison.OrdinalIgnoreCase));

        if (zoneA is not null)
        {
            WallLayer? floorInsulation = zoneA.Layers
                .FirstOrDefault(layer => TechnicalDocumentationDomainRules.IsThermalInsulationMaterial(layer.Material));

            if (floorInsulation is not null
                && !schedule.Insulation.Any(item => item.Element.Contains("podłog", StringComparison.OrdinalIgnoreCase)))
            {
                double thicknessCm = floorInsulation.ThicknessCm > 0 ? floorInsulation.ThicknessCm : 15;
                schedule.Insulation.Add(CreateCalculatedMaterialItem(
                    $"{floorInsulation.Material} — izolacja podłogi (strefa A)",
                    geometry.SlabAreaM2,
                    "m2",
                    InsulationWastePercent,
                    $"pow. strefy A {geometry.SlabAreaM2:F1} m² × gr. {thicknessCm:F0} cm",
                    "A-05",
                    "calculated"));
            }
        }

        if (geometry.FootprintM2 > 0
            && !schedule.Insulation.Any(item => item.Element.Contains("fundament", StringComparison.OrdinalIgnoreCase)))
        {
            schedule.Insulation.Add(CreateCalculatedMaterialItem(
                "Styropian EPS 100 — izolacja pozioma fundamentów",
                geometry.FootprintM2,
                "m2",
                InsulationWastePercent,
                $"pow. zabudowy {geometry.FootprintM2:F0} m² × gr. 10 cm",
                "A-05",
                "calculated"));
        }
    }

    private static List<FloorPlanDrawing> SelectSlabReinforcementDrawings(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<FloorPlanDrawing> reinforcementDrawings = drawings
            .Where(drawing => IsSlabReinforcementDrawing(drawing.Classification.DrawingType))
            .ToList();

        if (reinforcementDrawings.Count > 0)
        {
            return reinforcementDrawings;
        }

        return drawings
            .Where(drawing => drawing.Floors?.Steel.Count > 0 || drawing.Floors?.TotalMassKg is > 0)
            .ToList();
    }

    private static bool IsSlabReinforcementDrawing(string drawingType)
    {
        string type = drawingType.Trim().ToLowerInvariant();
        return type.Contains("zbrojenie_stropu", StringComparison.Ordinal)
            || type.Contains("zbrojenie stropu", StringComparison.Ordinal);
    }

    private static double ResolveSteelMassKg(FloorSection floors)
    {
        if (floors.TotalMassKg is > 0)
        {
            return floors.TotalMassKg.Value;
        }

        if (floors.Steel.Count > 0)
        {
            return floors.Steel.Sum(item => item.Quantity);
        }

        return floors.Bars.Sum(bar => bar.MassKg ?? 0);
    }

    private static double? ResolveSlabThicknessCm(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyDictionary<string, object> sharedState)
    {
        if (sharedState.TryGetValue("ceiling.thicknessCm", out object? value)
            && value is double thickness)
        {
            return thickness;
        }

        SlabDetail? slab = drawings
            .SelectMany(drawing => drawing.Floors?.Slabs ?? [])
            .FirstOrDefault(item => item.ThicknessCm > 0);

        return slab?.ThicknessCm;
    }

    private static string? ResolveSlabConcreteClass(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        return drawings
            .SelectMany(drawing => drawing.Floors?.Slabs ?? [])
            .Select(slab => slab.ConcreteClass)
            .FirstOrDefault(concrete => !string.IsNullOrWhiteSpace(concrete));
    }

    private static string ResolveSlabSourceSheets(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<string> sheets = drawings
            .Where(drawing => IsSlabReinforcementDrawing(drawing.Classification.DrawingType))
            .Select(drawing => drawing.Classification.SheetNumber)
            .Where(sheet => !string.IsNullOrWhiteSpace(sheet))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return sheets.Count == 0 ? "A-05, K-02, K-03" : string.Join(", ", sheets);
    }

    private static MaterialItem CreateReadMaterialItem(
        string element,
        double netQuantity,
        string unit,
        double wastePercent,
        string calculation,
        string sourceDrawing)
    {
        return CreateMaterialItem(element, netQuantity, unit, wastePercent, calculation, sourceDrawing, "read");
    }

    private static MaterialItem CreateCalculatedMaterialItem(
        string element,
        double netQuantity,
        string unit,
        double wastePercent,
        string calculation,
        string sourceDrawing,
        string sourceType)
    {
        return CreateMaterialItem(element, netQuantity, unit, wastePercent, calculation, sourceDrawing, sourceType);
    }

    private static MaterialItem CreateMaterialItem(
        string element,
        double netQuantity,
        string unit,
        double wastePercent,
        string calculation,
        string sourceDrawing,
        string sourceType)
    {
        return new MaterialItem
        {
            Element = element,
            Specification = element,
            NetQuantity = netQuantity,
            WastePercent = wastePercent,
            GrossQuantity = Math.Round(netQuantity * (1 + wastePercent / 100.0), 2),
            Unit = unit,
            SourceType = sourceType,
            Calculation = calculation,
            SourceDrawings = sourceDrawing
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        };
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
