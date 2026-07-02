using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class ConsolidatedProjectMaterials
{
    public List<MaterialQuantity> FoundationBlocks { get; set; } = new();
    public List<MaterialQuantity> FoundationConcrete { get; set; } = new();
    public List<MaterialQuantity> FoundationSteel { get; set; } = new();
    public List<MaterialQuantity> FloorConcrete { get; set; } = new();
    public List<MaterialQuantity> FloorSteel { get; set; } = new();
    public List<MaterialQuantity> WallMaterials { get; set; } = new();
    public List<MaterialQuantity> ThermalInsulation { get; set; } = new();
    public List<MaterialQuantity> Timber { get; set; } = new();
    public List<string> AuditNotes { get; set; } = new();
}

public static class DrawingMaterialConsolidator
{
    public static ConsolidatedProjectMaterials Consolidate(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies)
    {
        ConsolidatedProjectMaterials result = new();

        List<FloorPlanDrawing> planDrawings = FilterDrawings(drawings, DrawingViewBucket.Plan);
        List<FloorPlanDrawing> sectionDrawings = FilterDrawings(drawings, DrawingViewBucket.Section);
        List<FloorPlanDrawing> foundationDrawings = FilterDrawings(drawings, DrawingViewBucket.Foundation);
        List<FloorPlanDrawing> roofDrawings = FilterDrawings(drawings, DrawingViewBucket.Roof);
        List<FloorPlanDrawing> detailDrawings = FilterDrawings(drawings, DrawingViewBucket.Detail);

        if (planDrawings.Count == 0)
        {
            planDrawings = drawings
                .Where(drawing => DrawingViewClassifier.Classify(drawing.Classification) != DrawingViewBucket.Section)
                .ToList();
            result.AuditNotes.Add("Brak rzutów — konsolidacja materiałów z rysunków ogólnych.");
        }

        result.WallMaterials = ConsolidateWallMaterials(planDrawings, sectionDrawings, result.AuditNotes);
        result.FoundationBlocks = ConsolidateFoundationList(
            drawings,
            foundationDrawings,
            planDrawings,
            drawing => drawing.Foundations?.Blocks,
            MaterialUnitSection.FoundationBlocks,
            result.AuditNotes);
        result.FoundationConcrete = ConsolidateFoundationList(
            drawings,
            foundationDrawings,
            planDrawings,
            drawing => drawing.Foundations?.Concrete,
            MaterialUnitSection.FoundationConcrete,
            result.AuditNotes);
        result.FoundationSteel = ConsolidateFoundationList(
            drawings,
            foundationDrawings,
            planDrawings.Concat(detailDrawings).ToList(),
            drawing => drawing.Foundations?.Steel,
            MaterialUnitSection.FoundationSteel,
            result.AuditNotes);
        result.FloorConcrete = ConsolidateMaxPerMaterialKey(
            SelectSlabMaterialDrawings(drawings),
            drawing => drawing.Floors?.Concrete,
            MaterialUnitSection.FloorConcrete);
        result.FloorSteel = ConsolidateMaxPerMaterialKey(
            SelectSlabMaterialDrawings(drawings),
            drawing => drawing.Floors?.Steel,
            MaterialUnitSection.FloorSteel);
        result.ThermalInsulation = ConsolidateThermalInsulation(drawings, planDrawings, sectionDrawings, result.AuditNotes);
        result.Timber = ConsolidateTimber(roofDrawings, planDrawings, drawings, result.AuditNotes);

        _ = dependencies;
        return result;
    }

    private static List<MaterialQuantity> ConsolidateWallMaterials(
        IReadOnlyList<FloorPlanDrawing> planDrawings,
        IReadOnlyList<FloorPlanDrawing> sectionDrawings,
        List<string> auditNotes)
    {
        Dictionary<string, MaterialQuantity> consolidated = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in planDrawings)
        {
            AddWallMaterialsFromDrawing(consolidated, drawing, preferHigherQuantity: false, sumAcrossDrawings: true);
        }

        foreach (FloorPlanDrawing drawing in sectionDrawings)
        {
            AddWallMaterialsFromDrawing(consolidated, drawing, preferHigherQuantity: true, sumAcrossDrawings: false);
        }

        if (planDrawings.Count > 1)
        {
            auditNotes.Add($"Zsumowano materiały ścian z {planDrawings.Count} rzutów kondygnacji.");
        }

        if (sectionDrawings.Count > 0)
        {
            auditNotes.Add("Przekroje uzupełniają brakujące warstwy ścian bez podwajania rzutów.");
        }

        return MaterialQuantityFilter.Filter(consolidated.Values);
    }

    private static void AddWallMaterialsFromDrawing(
        Dictionary<string, MaterialQuantity> consolidated,
        FloorPlanDrawing drawing,
        bool preferHigherQuantity,
        bool sumAcrossDrawings)
    {
        foreach (Wall wall in drawing.Walls)
        {
            double wallArea = wall.NetAreaM2.HasValue && wall.NetAreaM2.Value > 0
                ? wall.NetAreaM2.Value
                : wall.GrossAreaM2 ?? 0;
            if (!MaterialQuantityFilter.HasPositiveQuantity(wallArea))
            {
                continue;
            }

            foreach (WallLayer layer in wall.Layers)
            {
                if (!TechnicalDocumentationDomainRules.IsWallLayerMaterial(layer.Material))
                {
                    continue;
                }

                if (TechnicalDocumentationDomainRules.IsThermalInsulationMaterial(layer.Material))
                {
                    continue;
                }

                string unit = MaterialUnitNormalizer.ResolveUnit(layer.Material, MaterialUnitSection.WallLayer);
                double quantity = MaterialUnitNormalizer.ResolveWallLayerQuantity(wallArea, layer, unit);
                if (!MaterialQuantityFilter.HasPositiveQuantity(quantity))
                {
                    continue;
                }

                string materialKey = TechnicalDocumentationDomainRules.NormalizeRoomName(layer.Material);
                string key = $"{materialKey}:{unit}";

                if (!consolidated.TryGetValue(key, out MaterialQuantity? existing))
                {
                    consolidated[key] = new MaterialQuantity
                    {
                        MaterialType = layer.Material,
                        Quantity = quantity,
                        Unit = unit
                    };

                    continue;
                }

                if (preferHigherQuantity)
                {
                    if (!consolidated.ContainsKey(key))
                    {
                        consolidated[key] = new MaterialQuantity
                        {
                            MaterialType = layer.Material,
                            Quantity = quantity,
                            Unit = unit
                        };
                    }

                    continue;
                }

                if (sumAcrossDrawings)
                {
                    existing.Quantity += quantity;
                }
            }
        }
    }

    private static List<MaterialQuantity> ConsolidateFoundationList(
        IReadOnlyList<FloorPlanDrawing> allDrawings,
        IReadOnlyList<FloorPlanDrawing> foundationDrawings,
        IReadOnlyList<FloorPlanDrawing> planDrawings,
        Func<FloorPlanDrawing, List<MaterialQuantity>?> selector,
        MaterialUnitSection section,
        List<string> auditNotes)
    {
        List<FloorPlanDrawing> sources = foundationDrawings.Count > 0
            ? foundationDrawings.ToList()
            : planDrawings.Count > 0
                ? planDrawings.ToList()
                : allDrawings.ToList();

        if (foundationDrawings.Count == 0 && planDrawings.Count > 0)
        {
            auditNotes.Add("Fundamenty skonsolidowane z rzutów — bez osobnych rysunków fundamentów.");
        }

        return ConsolidateMaxPerMaterialKey(sources, selector, section);
    }

    private static List<MaterialQuantity> ConsolidateThermalInsulation(
        IReadOnlyList<FloorPlanDrawing> allDrawings,
        IReadOnlyList<FloorPlanDrawing> planDrawings,
        IReadOnlyList<FloorPlanDrawing> sectionDrawings,
        List<string> auditNotes)
    {
        Dictionary<string, MaterialQuantity> consolidated = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in planDrawings.Concat(sectionDrawings))
        {
            if (drawing.Foundations?.Insulation is not null)
            {
                foreach (MaterialQuantity insulation in drawing.Foundations.Insulation)
                {
                    MergeMaxQuantity(
                        consolidated,
                        MaterialUnitNormalizer.Normalize(insulation, MaterialUnitSection.FoundationInsulation));
                }
            }

            foreach (Wall wall in drawing.Walls)
            {
                double wallArea = wall.NetAreaM2.HasValue && wall.NetAreaM2.Value > 0
                ? wall.NetAreaM2.Value
                : wall.GrossAreaM2 ?? 0;
                foreach (WallLayer layer in wall.Layers)
                {
                    if (!TechnicalDocumentationDomainRules.IsThermalInsulationMaterial(layer.Material))
                    {
                        continue;
                    }

                    MaterialQuantity item = MaterialUnitNormalizer.Normalize(
                        new MaterialQuantity
                        {
                            MaterialType = layer.Material,
                            Quantity = MaterialUnitNormalizer.ResolveWallLayerQuantity(
                                wallArea,
                                layer,
                                MaterialUnitNormalizer.ResolveUnit(layer.Material, MaterialUnitSection.WallInsulation)),
                            Unit = MaterialUnitNormalizer.ResolveUnit(layer.Material, MaterialUnitSection.WallInsulation)
                        },
                        MaterialUnitSection.WallInsulation);

                    MergeMaxQuantity(consolidated, item);
                }
            }
        }

        if (consolidated.Count == 0)
        {
            _ = allDrawings;
            return [];
        }

        auditNotes.Add("Izolacje skonsolidowane metodą MAX — bez sumowania tych samych warstw z wielu stron.");
        return MaterialQuantityFilter.Filter(consolidated.Values);
    }

    private static List<MaterialQuantity> ConsolidateTimber(
        IReadOnlyList<FloorPlanDrawing> roofDrawings,
        IReadOnlyList<FloorPlanDrawing> planDrawings,
        IReadOnlyList<FloorPlanDrawing> allDrawings,
        List<string> auditNotes)
    {
        List<FloorPlanDrawing> sources = roofDrawings.Count > 0
            ? roofDrawings.ToList()
            : allDrawings
                .Where(drawing => drawing.Roof?.Timber.Count > 0)
                .OrderByDescending(drawing => drawing.Roof?.AreaM2 ?? 0)
                .Take(1)
                .ToList();

        if (sources.Count == 0)
        {
            return [];
        }

        Dictionary<string, MaterialQuantity> consolidated = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in sources)
        {
            if (drawing.Roof?.Timber is null)
            {
                continue;
            }

            foreach (TimberElement timber in drawing.Roof.Timber)
            {
                double quantity = MaterialUnitNormalizer.ResolveTimberVolumeM3(timber);
                if (!MaterialQuantityFilter.HasPositiveQuantity(quantity))
                {
                    continue;
                }

                string key = $"{timber.Element}:{timber.Section}:{timber.WoodType}".ToLowerInvariant();
                MaterialQuantity item = MaterialUnitNormalizer.Normalize(
                    new MaterialQuantity
                    {
                        MaterialType = MaterialUnitNormalizer.FormatTimberMaterialLabel(timber),
                        Quantity = quantity,
                        Unit = "m3"
                    },
                    MaterialUnitSection.TimberElement);

                MergeMaxQuantity(consolidated, item, key);
            }
        }

        if (planDrawings.Count > 0 && roofDrawings.Count == 0)
        {
            auditNotes.Add("Drewno konstrukcyjne pobrane z jednego rysunku dachu/więźby — bez sumowania stron.");
        }

        return MaterialQuantityFilter.Filter(consolidated.Values);
    }

    private static List<MaterialQuantity> ConsolidateMaxPerMaterialKey(
        IReadOnlyList<FloorPlanDrawing> drawings,
        Func<FloorPlanDrawing, List<MaterialQuantity>?> selector,
        MaterialUnitSection section)
    {
        Dictionary<string, MaterialQuantity> consolidated = new(StringComparer.OrdinalIgnoreCase);

        foreach (FloorPlanDrawing drawing in drawings)
        {
            List<MaterialQuantity>? materials = selector(drawing);
            if (materials is null)
            {
                continue;
            }

            foreach (MaterialQuantity material in materials)
            {
                if (section == MaterialUnitSection.FoundationBlocks
                    && !TechnicalDocumentationDomainRules.IsFoundationBlockMaterial(material.MaterialType))
                {
                    continue;
                }

                MergeMaxQuantity(consolidated, MaterialUnitNormalizer.Normalize(material, section));
            }
        }

        return MaterialQuantityFilter.Filter(consolidated.Values);
    }

    private static void MergeMaxQuantity(
        Dictionary<string, MaterialQuantity> consolidated,
        MaterialQuantity item,
        string? explicitKey = null)
    {
        if (!MaterialQuantityFilter.HasPositiveQuantity(item.Quantity))
        {
            return;
        }

        string key = explicitKey ?? $"{item.MaterialType}:{item.Unit}".ToLowerInvariant();
        if (!consolidated.TryGetValue(key, out MaterialQuantity? existing))
        {
            consolidated[key] = new MaterialQuantity
            {
                MaterialType = item.MaterialType,
                Quantity = item.Quantity,
                Unit = item.Unit
            };

            return;
        }

        if (item.Quantity > existing.Quantity)
        {
            existing.Quantity = item.Quantity;
            existing.MaterialType = item.MaterialType;
        }
    }

    private static List<FloorPlanDrawing> SelectFloorDrawings(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<FloorPlanDrawing> planDrawings = FilterDrawings(drawings, DrawingViewBucket.Plan);
        if (planDrawings.Count > 0)
        {
            return planDrawings;
        }

        return drawings.ToList();
    }

    private static List<FloorPlanDrawing> SelectSlabMaterialDrawings(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<FloorPlanDrawing> reinforcementDrawings = drawings
            .Where(drawing => IsSlabReinforcementDrawing(drawing.Classification.DrawingType))
            .ToList();

        if (reinforcementDrawings.Count > 0)
        {
            return reinforcementDrawings;
        }

        List<FloorPlanDrawing> floorDataDrawings = drawings
            .Where(drawing => drawing.Floors?.Steel.Count > 0
                || drawing.Floors?.Concrete.Count > 0
                || drawing.Floors?.TotalMassKg is > 0)
            .ToList();

        if (floorDataDrawings.Count > 0)
        {
            return floorDataDrawings;
        }

        return SelectFloorDrawings(drawings);
    }

    private static bool IsSlabReinforcementDrawing(string drawingType)
    {
        string type = drawingType.Trim().ToLowerInvariant();
        return type.Contains("zbrojenie_stropu", StringComparison.Ordinal)
            || type.Contains("zbrojenie stropu", StringComparison.Ordinal);
    }

    private static List<FloorPlanDrawing> FilterDrawings(
        IReadOnlyList<FloorPlanDrawing> drawings,
        DrawingViewBucket bucket)
    {
        return drawings
            .Where(drawing => DrawingViewClassifier.Classify(drawing.Classification) == bucket)
            .ToList();
    }
}
