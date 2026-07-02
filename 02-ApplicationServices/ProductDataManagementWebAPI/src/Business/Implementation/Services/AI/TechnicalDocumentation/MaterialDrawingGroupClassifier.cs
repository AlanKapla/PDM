using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class MaterialDrawingGroupClassifier
{
    private static readonly HashSet<string> FoundationDrawingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rzut_fundamentow"
    };

    private static readonly HashSet<string> CeilingDrawingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "zbrojenie_stropu_dolne",
        "zbrojenie_stropu_gorne"
    };

    private static readonly HashSet<string> RoofDrawingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rzut_dachu",
        "rzut_wiezby_dachowej",
        "aksonometria_wiezby"
    };

    private static readonly HashSet<string> WallFloorPlanDrawingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rzut_parteru",
        "rzut_piętra",
        "rzut_pietra",
        "rzut_poddasza",
        "rzut_piwnicy"
    };

    private static readonly HashSet<string> StructuralDetailDrawingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "detale_konstrukcyjne",
        "detal_konstrukcyjne",
        "detale_konstrukcyjny",
        "detal_konstrukcyjny"
    };

    public static bool QualifiesForGroup(FloorPlanDrawing drawing, MaterialDrawingGroupKind kind)
    {
        return kind switch
        {
            MaterialDrawingGroupKind.Foundations => IsFoundationDomain(drawing),
            MaterialDrawingGroupKind.Ceilings => IsCeilingDomain(drawing),
            MaterialDrawingGroupKind.Roof => IsRoofDomain(drawing),
            MaterialDrawingGroupKind.Walls => IsWallDomain(drawing),
            _ => false
        };
    }

    public static bool QualifiesAsSectionContext(FloorPlanDrawing drawing)
    {
        string normalizedType = NormalizeDrawingType(drawing.Classification.DrawingType);
        DrawingViewBucket bucket = DrawingViewClassifier.Classify(drawing.Classification);

        if (normalizedType == "przekroj" || bucket == DrawingViewBucket.Section)
        {
            return true;
        }

        return drawing.Section is not null
            && (drawing.Section.Levels is not null || drawing.Section.FloorZones.Count > 0);
    }

    public static bool DetailReferencesGroup(
        string? detailType,
        string? referenceLabel,
        MaterialDrawingGroupKind kind)
    {
        string combined = NormalizeSemanticText($"{detailType} {referenceLabel}");

        return kind switch
        {
            MaterialDrawingGroupKind.Foundations => ContainsAny(
                combined,
                "fundament",
                "law",
                "lawa",
                "stopa",
                "slup",
                "słup",
                "zbrojenie slup",
                "zbrojenie słup",
                "lawy",
                "lawy zelbet"),
            MaterialDrawingGroupKind.Ceilings => ContainsAny(
                combined,
                "strop",
                "zbrojenie strop",
                "zbrojenie_strop",
                "plyta",
                "płyta",
                "strop zelbet",
                "strop zelbetowy"),
            MaterialDrawingGroupKind.Roof => ContainsAny(
                combined,
                "dach",
                "wiezba",
                "więźba",
                "krokiew",
                "pokrycie dach",
                "murłata",
                "murlata"),
            MaterialDrawingGroupKind.Walls => ContainsAny(
                combined,
                "scian",
                "ścian",
                "elewac",
                "rzut",
                "przekroj",
                "przekrój",
                "parter",
                "poddasze",
                "pietro",
                "piętro"),
            _ => false
        };
    }

    private static bool IsFoundationDomain(FloorPlanDrawing drawing)
    {
        string normalizedType = NormalizeDrawingType(drawing.Classification.DrawingType);
        DrawingViewBucket bucket = DrawingViewClassifier.Classify(drawing.Classification);

        if (FoundationDrawingTypes.Contains(normalizedType) || bucket == DrawingViewBucket.Foundation)
        {
            return true;
        }

        if (HasFoundationData(drawing))
        {
            return true;
        }

        if (IsStructuralDetailDrawing(normalizedType)
            && (DetailReferencesGroup(
                    BuildCombinedText(drawing.Classification),
                    drawing.Classification.DrawingType,
                    MaterialDrawingGroupKind.Foundations)
                || ContainsAny(BuildCombinedText(drawing.Classification), "fundament")))
        {
            return true;
        }

        return ContainsAny(
            BuildCombinedText(drawing.Classification),
            "fundament",
            "law",
            "lawa",
            "stopa fundamentowa");
    }

    private static bool IsCeilingDomain(FloorPlanDrawing drawing)
    {
        string normalizedType = NormalizeDrawingType(drawing.Classification.DrawingType);

        if (CeilingDrawingTypes.Contains(normalizedType))
        {
            return true;
        }

        if (HasCeilingData(drawing))
        {
            return true;
        }

        if (IsStructuralDetailDrawing(normalizedType)
            && DetailReferencesGroup(drawing.Classification.Title, drawing.Classification.DrawingType, MaterialDrawingGroupKind.Ceilings))
        {
            return true;
        }

        return ContainsAny(
            BuildCombinedText(drawing.Classification),
            "zbrojenie_stropu",
            "zbrojenie stropu",
            "strop zelbet",
            "strop");
    }

    private static bool IsRoofDomain(FloorPlanDrawing drawing)
    {
        string normalizedType = NormalizeDrawingType(drawing.Classification.DrawingType);
        DrawingViewBucket bucket = DrawingViewClassifier.Classify(drawing.Classification);

        if (RoofDrawingTypes.Contains(normalizedType) || bucket == DrawingViewBucket.Roof)
        {
            return true;
        }

        if (HasRoofData(drawing))
        {
            return true;
        }

        return ContainsAny(
            BuildCombinedText(drawing.Classification),
            "dach",
            "wiezba",
            "więźba",
            "krokiew",
            "pokrycie dach");
    }

    private static bool IsWallDomain(FloorPlanDrawing drawing)
    {
        string normalizedType = NormalizeDrawingType(drawing.Classification.DrawingType);
        DrawingViewBucket bucket = DrawingViewClassifier.Classify(drawing.Classification);

        if (normalizedType == "elewacja" || bucket == DrawingViewBucket.Elevation)
        {
            return true;
        }

        if (WallFloorPlanDrawingTypes.Contains(normalizedType))
        {
            return true;
        }

        if (IsRoofDomain(drawing) || IsFoundationDomain(drawing))
        {
            return false;
        }

        if (bucket == DrawingViewBucket.Plan && (drawing.Rooms.Count > 0 || drawing.Walls.Count > 0))
        {
            return true;
        }

        return ContainsAny(
            BuildCombinedText(drawing.Classification),
            "rzut parter",
            "rzut poddasz",
            "rzut pietr",
            "rzut piętr",
            "rzut piwnic",
            "elewacja");
    }

    private static bool HasFoundationData(FloorPlanDrawing drawing)
    {
        return drawing.Foundations?.Footings.Count > 0
            || drawing.Foundations?.Concrete.Count > 0
            || drawing.Foundations?.Steel.Count > 0
            || drawing.Foundations?.Blocks.Count > 0
            || drawing.Columns.Count > 0;
    }

    private static bool HasCeilingData(FloorPlanDrawing drawing)
    {
        return drawing.Floors?.Steel.Count > 0
            || drawing.Floors?.Concrete.Count > 0
            || drawing.Floors?.Slabs.Count > 0;
    }

    private static bool HasRoofData(FloorPlanDrawing drawing)
    {
        return drawing.Roof?.AreaM2 > 0
            || drawing.Roof?.Timber.Count > 0
            || !string.IsNullOrWhiteSpace(drawing.Roof?.CoveringType);
    }

    private static bool IsStructuralDetailDrawing(string normalizedType)
    {
        return StructuralDetailDrawingTypes.Contains(normalizedType);
    }

    private static string NormalizeDrawingType(string? drawingType)
    {
        if (string.IsNullOrWhiteSpace(drawingType))
        {
            return string.Empty;
        }

        return drawingType.Trim().ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('ł', 'l')
            .Replace('ó', 'o')
            .Replace('ą', 'a')
            .Replace('ę', 'e')
            .Replace('ś', 's')
            .Replace('ć', 'c')
            .Replace('ń', 'n')
            .Replace('ź', 'z')
            .Replace('ż', 'z');
    }

    private static string BuildCombinedText(DrawingClassification classification)
    {
        return NormalizeSemanticText(
            $"{classification.DrawingType} {classification.Title} {classification.DescriptiveText}");
    }

    private static string NormalizeSemanticText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant()
            .Replace('ł', 'l')
            .Replace('ó', 'o')
            .Replace('ą', 'a')
            .Replace('ę', 'e')
            .Replace('ś', 's')
            .Replace('ć', 'c')
            .Replace('ń', 'n')
            .Replace('ź', 'z')
            .Replace('ż', 'z');
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
