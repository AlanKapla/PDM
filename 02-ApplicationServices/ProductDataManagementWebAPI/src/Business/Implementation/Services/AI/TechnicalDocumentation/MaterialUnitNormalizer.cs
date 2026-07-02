using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public enum MaterialUnitSection
{
    Generic,
    FoundationBlocks,
    FoundationConcrete,
    FoundationSteel,
    FoundationInsulation,
    FloorConcrete,
    FloorSteel,
    WallLayer,
    WallMasonry,
    WallMortar,
    WallInsulation,
    CeilingConcrete,
    CeilingSteel,
    RoofCovering,
    RoofTimber,
    RoofInsulation,
    TimberElement
}

public static class MaterialUnitNormalizer
{
    public static string ResolveUnit(
        string materialType,
        MaterialUnitSection section = MaterialUnitSection.Generic,
        string? currentUnit = null)
    {
        string? sectionUnit = ResolveFromSection(section);
        if (sectionUnit is not null)
        {
            return sectionUnit;
        }

        string? materialUnit = ResolveFromMaterialType(materialType, currentUnit);
        if (materialUnit is not null)
        {
            return materialUnit;
        }

        return NormalizeUnitString(currentUnit) is { Length: > 0 } normalized
            ? normalized
            : "szt";
    }

    public static MaterialQuantity Normalize(MaterialQuantity item, MaterialUnitSection section = MaterialUnitSection.Generic)
    {
        item.Unit = ResolveUnit(item.MaterialType, section, item.Unit);
        return item;
    }

    public static MaterialSummaryItem Normalize(MaterialSummaryItem item)
    {
        item.Unit = ResolveUnit(item.MaterialType, MaterialUnitSection.Generic, item.Unit);
        return item;
    }

    public static MaterialItem Normalize(MaterialItem item, MaterialUnitSection section)
    {
        string materialName = !string.IsNullOrWhiteSpace(item.Specification)
            ? item.Specification
            : item.Element;
        item.Unit = ResolveUnit(materialName, section, item.Unit);
        return item;
    }

    public static double ResolveWallLayerQuantity(double wallAreaM2, WallLayer layer, string unit)
    {
        if (unit == "m3" && layer.ThicknessCm > 0)
        {
            return Math.Round(wallAreaM2 * layer.ThicknessCm / 100.0, 2);
        }

        if (unit == "m2")
        {
            return Math.Round(wallAreaM2, 2);
        }

        return 0;
    }

    public static string FormatTimberMaterialLabel(TimberElement timber)
    {
        List<string> parts = new();

        if (!string.IsNullOrWhiteSpace(timber.Element))
        {
            parts.Add(timber.Element.Trim());
        }

        if (!string.IsNullOrWhiteSpace(timber.Section))
        {
            parts.Add(timber.Section.Trim());
        }

        if (!string.IsNullOrWhiteSpace(timber.WoodType))
        {
            parts.Add(timber.WoodType.Trim());
        }

        return string.Join(" ", parts);
    }

    public static double ResolveTimberVolumeM3(TimberElement timber)
    {
        if (!TryParseTimberSectionCm(timber.Section, out double widthCm, out double heightCm))
        {
            return 0;
        }

        if (timber.LengthM <= 0)
        {
            return 0;
        }

        int count = timber.Count > 0 ? timber.Count : 1;
        double crossSectionM2 = (widthCm / 100.0) * (heightCm / 100.0);
        return Math.Round(count * timber.LengthM * crossSectionM2, 3);
    }

    public static bool TryParseTimberSectionCm(string? section, out double widthCm, out double heightCm)
    {
        widthCm = 0;
        heightCm = 0;

        if (string.IsNullOrWhiteSpace(section))
        {
            return false;
        }

        string normalized = section.Trim().ToLowerInvariant()
            .Replace("cm", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace('/', 'x');

        int separatorIndex = normalized.IndexOf('x');
        if (separatorIndex <= 0 || separatorIndex >= normalized.Length - 1)
        {
            return false;
        }

        string widthPart = normalized[..separatorIndex];
        string heightPart = normalized[(separatorIndex + 1)..];

        if (!double.TryParse(widthPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out widthCm)
            || !double.TryParse(heightPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out heightCm))
        {
            return false;
        }

        return widthCm > 0 && heightCm > 0;
    }

    public static MaterialSchedule NormalizeSchedule(MaterialSchedule schedule)
    {
        schedule.Summary = schedule.Summary.Select(Normalize).ToList();

        schedule.Masonry = NormalizeMaterialItems(schedule.Masonry, MaterialUnitSection.WallMasonry);
        schedule.Insulation = NormalizeMaterialItems(schedule.Insulation, MaterialUnitSection.WallInsulation);
        schedule.Concrete = NormalizeMaterialItems(schedule.Concrete, MaterialUnitSection.FoundationConcrete);
        schedule.Steel = NormalizeMaterialItems(schedule.Steel, MaterialUnitSection.FoundationSteel);
        schedule.Timber = NormalizeMaterialItems(schedule.Timber, MaterialUnitSection.RoofTimber);
        schedule.Roofing = NormalizeMaterialItems(schedule.Roofing, MaterialUnitSection.RoofCovering);
        schedule.Finishes = NormalizeMaterialItems(schedule.Finishes, MaterialUnitSection.WallMortar);

        schedule.Foundations.Concrete = NormalizeMaterialItems(schedule.Foundations.Concrete, MaterialUnitSection.FoundationConcrete);
        schedule.Foundations.Steel = NormalizeMaterialItems(schedule.Foundations.Steel, MaterialUnitSection.FoundationSteel);
        schedule.Foundations.Blocks = NormalizeMaterialItems(schedule.Foundations.Blocks, MaterialUnitSection.FoundationBlocks);

        schedule.Walls.Masonry = NormalizeMaterialItems(schedule.Walls.Masonry, MaterialUnitSection.WallMasonry);
        schedule.Walls.Mortar = NormalizeMaterialItems(schedule.Walls.Mortar, MaterialUnitSection.WallMortar);
        schedule.Walls.Insulation = NormalizeMaterialItems(schedule.Walls.Insulation, MaterialUnitSection.WallInsulation);

        schedule.Ceilings.Concrete = NormalizeMaterialItems(schedule.Ceilings.Concrete, MaterialUnitSection.CeilingConcrete);
        schedule.Ceilings.Steel = NormalizeMaterialItems(schedule.Ceilings.Steel, MaterialUnitSection.CeilingSteel);

        schedule.Columns.Concrete = NormalizeMaterialItems(schedule.Columns.Concrete, MaterialUnitSection.CeilingConcrete);
        schedule.Columns.Steel = NormalizeMaterialItems(schedule.Columns.Steel, MaterialUnitSection.CeilingSteel);

        schedule.Roof.Covering = NormalizeMaterialItems(schedule.Roof.Covering, MaterialUnitSection.RoofCovering);
        schedule.Roof.Timber = NormalizeMaterialItems(schedule.Roof.Timber, MaterialUnitSection.RoofTimber);
        schedule.Roof.Insulation = NormalizeMaterialItems(schedule.Roof.Insulation, MaterialUnitSection.RoofInsulation);

        return schedule;
    }

    public static FloorPlanDrawing NormalizeDrawing(FloorPlanDrawing drawing)
    {
        if (drawing.Foundations is not null)
        {
            drawing.Foundations.Blocks = NormalizeQuantities(drawing.Foundations.Blocks, MaterialUnitSection.FoundationBlocks);
            drawing.Foundations.Concrete = NormalizeQuantities(drawing.Foundations.Concrete, MaterialUnitSection.FoundationConcrete);
            drawing.Foundations.Steel = NormalizeQuantities(drawing.Foundations.Steel, MaterialUnitSection.FoundationSteel);
            drawing.Foundations.Insulation = NormalizeQuantities(drawing.Foundations.Insulation, MaterialUnitSection.FoundationInsulation);
        }

        if (drawing.Floors is not null)
        {
            drawing.Floors.Concrete = NormalizeQuantities(drawing.Floors.Concrete, MaterialUnitSection.FloorConcrete);
            drawing.Floors.Steel = NormalizeQuantities(drawing.Floors.Steel, MaterialUnitSection.FloorSteel);
        }

        return drawing;
    }

    private static List<MaterialQuantity> NormalizeQuantities(
        List<MaterialQuantity> items,
        MaterialUnitSection section)
    {
        return items.Select(item => Normalize(item, section)).ToList();
    }

    private static List<MaterialItem> NormalizeMaterialItems(
        List<MaterialItem> items,
        MaterialUnitSection section)
    {
        return items.Select(item => Normalize(item, section)).ToList();
    }

    private static string? ResolveFromSection(MaterialUnitSection section)
    {
        return section switch
        {
            MaterialUnitSection.FoundationBlocks => "szt",
            MaterialUnitSection.FoundationConcrete or MaterialUnitSection.FloorConcrete or MaterialUnitSection.CeilingConcrete => "m3",
            MaterialUnitSection.FoundationSteel or MaterialUnitSection.FloorSteel or MaterialUnitSection.CeilingSteel => "kg",
            MaterialUnitSection.FoundationInsulation or MaterialUnitSection.WallInsulation or MaterialUnitSection.RoofInsulation => "m2",
            MaterialUnitSection.RoofTimber or MaterialUnitSection.TimberElement => "m3",
            MaterialUnitSection.WallMortar or MaterialUnitSection.RoofCovering => "m2",
            _ => null
        };
    }

    private static string? ResolveFromMaterialType(string materialType, string? currentUnit)
    {
        string text = materialType.Trim().ToLowerInvariant();

        if (ContainsAny(text, "bloczek", "pustak", " ceg", "cegł", "cegl"))
        {
            return "szt";
        }

        if (ContainsAny(text, "beton", "c20/", "c25/", "c30/", "c16/", "ław"))
        {
            return "m3";
        }

        if (ContainsAny(text, "stal", "zbroj", "ø", "q188", "q335", "q524", "siatka q", "pręt", "pret", "drut"))
        {
            return "kg";
        }

        if (ContainsAny(text, "styropian", "xps", "eps", "wełna", "welna", "grafit", "izolac", "polistyren"))
        {
            return "m2";
        }

        if (ContainsAny(text, "tynk", "zaprawa", "murarsk", "gładź", "gladz"))
        {
            return "m2";
        }

        if (ContainsAny(text, "dachówk", "dachowk", "blachodach", "blacha", "pokrycie dach", "papa"))
        {
            return "m2";
        }

        if (ContainsAny(text, "krokw", "murłat", "murlat", "płatwi", "platwi", "kontrłat", "kontrlat"))
        {
            return "m3";
        }

        if (ContainsAny(text, "łat", " lat "))
        {
            return "m3";
        }

        if (ContainsAny(text, "beton komórkowy", "beton komorkowy", "ytong", "porotherm", "silikat", "keramzyt"))
        {
            return "m3";
        }

        return null;
    }

    private static string NormalizeUnitString(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return string.Empty;
        }

        return unit.Trim().ToLowerInvariant()
            .Replace("m²", "m2", StringComparison.Ordinal)
            .Replace("m³", "m3", StringComparison.Ordinal)
            .Replace("m.b.", "mb", StringComparison.Ordinal)
            .Replace("mb.", "mb", StringComparison.Ordinal)
            .Replace("szt.", "szt", StringComparison.Ordinal)
            .Replace("kilogram", "kg", StringComparison.Ordinal)
            .Replace("kilogramy", "kg", StringComparison.Ordinal);
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
