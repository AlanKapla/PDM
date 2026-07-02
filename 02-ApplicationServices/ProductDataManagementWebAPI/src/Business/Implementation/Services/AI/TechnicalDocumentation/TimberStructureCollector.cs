using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TimberStructureCollector
{
    public static List<TimberGroup> CollectGroups(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        List<TimberGroup> groups = drawings
            .SelectMany(drawing => drawing.Roof?.TimberGroups ?? [])
            .Where(group => group.Rows.Count > 0 || group.GroupVolumeM3 > 0)
            .ToList();

        if (groups.Count > 0)
        {
            return groups;
        }

        List<TimberElement> timberElements = drawings
            .SelectMany(drawing => drawing.Roof?.Timber ?? [])
            .Where(element => !string.IsNullOrWhiteSpace(element.Element) || !string.IsNullOrWhiteSpace(element.Section))
            .ToList();

        if (timberElements.Count == 0)
        {
            return [];
        }

        return timberElements
            .GroupBy(element => $"{element.Element}|{element.Section}", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                TimberElement first = group.First();
                return new TimberGroup
                {
                    Name = first.Element,
                    Section = first.Section,
                    GroupSumMb = group.Sum(item => item.RowSumMb ?? item.LengthM * Math.Max(item.Count, 1)),
                    GroupVolumeM3 = group.Sum(item => item.VolumeM3 ?? 0),
                    Rows = group
                        .Select(item => new TimberGroupRow
                        {
                            Count = item.Count,
                            LengthM = item.LengthM,
                            RowSumMb = item.RowSumMb ?? item.LengthM * Math.Max(item.Count, 1)
                        })
                        .ToList()
                };
            })
            .ToList();
    }
}
