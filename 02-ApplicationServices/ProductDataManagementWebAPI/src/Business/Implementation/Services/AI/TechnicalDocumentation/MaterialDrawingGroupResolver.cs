using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class MaterialDrawingGroupResolver
{
    public static IReadOnlyList<MaterialDrawingGroup> Resolve(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies)
    {
        if (drawings.Count == 0)
        {
            return [];
        }

        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys = new()
        {
            [MaterialDrawingGroupKind.Foundations] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [MaterialDrawingGroupKind.Walls] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [MaterialDrawingGroupKind.Ceilings] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [MaterialDrawingGroupKind.Roof] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (FloorPlanDrawing drawing in drawings)
        {
            AssignSeedGroups(drawing, groupKeys);
        }

        ExpandWithDependencies(drawings, dependencies, groupKeys);
        ExpandWithRelatedDrawings(drawings, groupKeys);
        AttachSectionContextDrawings(drawings, groupKeys);

        return
        [
            BuildGroup(MaterialDrawingGroupKind.Foundations, "Fundamenty", drawings, groupKeys),
            BuildGroup(MaterialDrawingGroupKind.Walls, "Ściany", drawings, groupKeys),
            BuildGroup(MaterialDrawingGroupKind.Ceilings, "Stropy", drawings, groupKeys),
            BuildGroup(MaterialDrawingGroupKind.Roof, "Dach", drawings, groupKeys)
        ];
    }

    private static void AssignSeedGroups(
        FloorPlanDrawing drawing,
        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys)
    {
        string key = BuildDrawingKey(drawing);

        foreach (MaterialDrawingGroupKind kind in Enum.GetValues<MaterialDrawingGroupKind>())
        {
            if (MaterialDrawingGroupClassifier.QualifiesForGroup(drawing, kind))
            {
                groupKeys[kind].Add(key);
            }
        }
    }

    private static void ExpandWithDependencies(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink> dependencies,
        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys)
    {
        bool changed = true;

        while (changed)
        {
            changed = false;

            foreach (DrawingDependencyLink link in dependencies)
            {
                foreach (MaterialDrawingGroupKind kind in Enum.GetValues<MaterialDrawingGroupKind>())
                {
                    HashSet<string> members = groupKeys[kind];
                    FloorPlanDrawing? sourceDrawing = FindDrawing(drawings, link.SourceFileName, link.SourcePageNumber);
                    FloorPlanDrawing? targetDrawing = FindDrawing(drawings, link.TargetFileName, link.TargetPageNumber);
                    string? sourceKey = sourceDrawing is null ? null : BuildDrawingKey(sourceDrawing);
                    string? targetKey = targetDrawing is null ? null : BuildDrawingKey(targetDrawing);

                    if (TryAddLinkedDrawing(
                            members,
                            sourceKey,
                            targetKey,
                            targetDrawing,
                            kind,
                            link.DetailType,
                            link.ReferenceLabel))
                    {
                        changed = true;
                    }

                    if (TryAddLinkedDrawing(
                            members,
                            targetKey,
                            sourceKey,
                            sourceDrawing,
                            kind,
                            link.DetailType,
                            link.ReferenceLabel))
                    {
                        changed = true;
                    }
                }
            }
        }
    }

    private static void ExpandWithRelatedDrawings(
        IReadOnlyList<FloorPlanDrawing> drawings,
        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys)
    {
        foreach (FloorPlanDrawing drawing in drawings)
        {
            string sourceKey = BuildDrawingKey(drawing);
            MaterialDrawingGroupKind? sourceKind = ResolveMemberKind(sourceKey, groupKeys);

            if (sourceKind is null)
            {
                continue;
            }

            foreach (RelatedDrawingRef reference in drawing.Classification.RelatedDrawings)
            {
                FloorPlanDrawing? related = FindDrawingBySheetOrTitle(
                    drawings,
                    reference.TargetSheetNumber,
                    reference.TargetTitle);

                if (related is null)
                {
                    continue;
                }

                string relatedKey = BuildDrawingKey(related);
                HashSet<string> members = groupKeys[sourceKind.Value];

                if (members.Contains(relatedKey))
                {
                    continue;
                }

                if (MaterialDrawingGroupClassifier.QualifiesForGroup(related, sourceKind.Value)
                    || MaterialDrawingGroupClassifier.DetailReferencesGroup(
                        reference.DetailType,
                        reference.ReferenceLabel,
                        sourceKind.Value))
                {
                    members.Add(relatedKey);
                }
            }
        }
    }

    private static void AttachSectionContextDrawings(
        IReadOnlyList<FloorPlanDrawing> drawings,
        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys)
    {
        List<FloorPlanDrawing> sectionDrawings = drawings
            .Where(MaterialDrawingGroupClassifier.QualifiesAsSectionContext)
            .ToList();

        if (sectionDrawings.Count == 0)
        {
            return;
        }

        MaterialDrawingGroupKind[] groupsNeedingSections =
        [
            MaterialDrawingGroupKind.Foundations,
            MaterialDrawingGroupKind.Walls,
            MaterialDrawingGroupKind.Ceilings
        ];

        foreach (MaterialDrawingGroupKind kind in groupsNeedingSections)
        {
            if (groupKeys[kind].Count == 0)
            {
                continue;
            }

            foreach (FloorPlanDrawing sectionDrawing in sectionDrawings)
            {
                groupKeys[kind].Add(BuildDrawingKey(sectionDrawing));
            }
        }
    }

    private static bool TryAddLinkedDrawing(
        HashSet<string> members,
        string? memberKey,
        string? linkedKey,
        FloorPlanDrawing? linkedDrawing,
        MaterialDrawingGroupKind kind,
        string? detailType,
        string? referenceLabel)
    {
        if (memberKey is null
            || linkedKey is null
            || linkedDrawing is null
            || !members.Contains(memberKey)
            || members.Contains(linkedKey))
        {
            return false;
        }

        if (MaterialDrawingGroupClassifier.QualifiesForGroup(linkedDrawing, kind)
            || MaterialDrawingGroupClassifier.DetailReferencesGroup(detailType, referenceLabel, kind))
        {
            members.Add(linkedKey);
            return true;
        }

        return false;
    }

    private static MaterialDrawingGroup BuildGroup(
        MaterialDrawingGroupKind kind,
        string label,
        IReadOnlyList<FloorPlanDrawing> drawings,
        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys)
    {
        HashSet<string> keys = groupKeys[kind];
        List<FloorPlanDrawing> members = drawings
            .Where(drawing => keys.Contains(BuildDrawingKey(drawing)))
            .OrderBy(drawing => drawing.Classification.SheetNumber ?? drawing.Source.FileName)
            .ToList();

        return new MaterialDrawingGroup
        {
            Kind = kind,
            Label = label,
            Drawings = members
        };
    }

    private static MaterialDrawingGroupKind? ResolveMemberKind(
        string drawingKey,
        Dictionary<MaterialDrawingGroupKind, HashSet<string>> groupKeys)
    {
        foreach (KeyValuePair<MaterialDrawingGroupKind, HashSet<string>> entry in groupKeys)
        {
            if (entry.Value.Contains(drawingKey))
            {
                return entry.Key;
            }
        }

        return null;
    }

    private static string BuildDrawingKey(FloorPlanDrawing drawing)
    {
        return $"{drawing.Source.FileName}::{drawing.Source.PageNumber}";
    }

    private static FloorPlanDrawing? FindDrawing(
        IReadOnlyList<FloorPlanDrawing> drawings,
        string? fileName,
        int? pageNumber)
    {
        if (string.IsNullOrWhiteSpace(fileName) || pageNumber is null)
        {
            return null;
        }

        return drawings.FirstOrDefault(candidate =>
            string.Equals(candidate.Source.FileName, fileName, StringComparison.OrdinalIgnoreCase)
            && candidate.Source.PageNumber == pageNumber.Value);
    }

    private static FloorPlanDrawing? FindDrawingBySheetOrTitle(
        IReadOnlyList<FloorPlanDrawing> drawings,
        string? sheetNumber,
        string? title)
    {
        if (!string.IsNullOrWhiteSpace(sheetNumber))
        {
            FloorPlanDrawing? bySheet = drawings.FirstOrDefault(drawing =>
                string.Equals(
                    drawing.Classification.SheetNumber,
                    sheetNumber,
                    StringComparison.OrdinalIgnoreCase));

            if (bySheet is not null)
            {
                return bySheet;
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return drawings.FirstOrDefault(drawing =>
            string.Equals(drawing.Classification.Title, title, StringComparison.OrdinalIgnoreCase));
    }
}
