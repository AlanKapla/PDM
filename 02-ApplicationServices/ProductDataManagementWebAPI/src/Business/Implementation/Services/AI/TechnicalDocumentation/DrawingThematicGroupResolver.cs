using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public sealed class DrawingThematicGroupResolver
{
    private readonly TechnicalDocumentationOptions options;

    public DrawingThematicGroupResolver(IOptions<TechnicalDocumentationOptions> options)
    {
        this.options = options.Value;
    }

    public IReadOnlyList<ThematicDrawingGroup> Resolve(
        IReadOnlyList<ClassifiedTechnicalDocumentationImage> classifiedImages)
    {
        if (classifiedImages.Count == 0)
        {
            return [];
        }

        IReadOnlyDictionary<string, string[]> mapping = options.GetEffectiveDrawingTypeToThematicGroups();
        Dictionary<string, List<ClassifiedTechnicalDocumentationImage>> groups = new(StringComparer.Ordinal);

        foreach (ClassifiedTechnicalDocumentationImage classifiedImage in classifiedImages)
        {
            string drawingType = classifiedImage.Classification.DrawingType ?? TechnicalDocumentationOptions.DrawingTypes.Nieznany;

            if (!mapping.TryGetValue(drawingType, out string[]? groupNames) || groupNames.Length == 0)
            {
                groupNames = [TechnicalDocumentationOptions.ThematicGroups.Other];
            }

            foreach (string groupName in groupNames)
            {
                if (!groups.TryGetValue(groupName, out List<ClassifiedTechnicalDocumentationImage>? members))
                {
                    members = [];
                    groups[groupName] = members;
                }

                if (!members.Any(existing => IsSameImage(existing.Image, classifiedImage.Image)))
                {
                    members.Add(classifiedImage);
                }
            }
        }

        return groups
            .OrderBy(entry => ResolveGroupOrder(entry.Key))
            .Select(entry => new ThematicDrawingGroup
            {
                GroupName = entry.Key,
                Images = entry.Value
                    .OrderBy(image => image.Classification.SheetNumber ?? image.Image.FileName)
                    .ToList(),
            })
            .ToList();
    }

    public static string BuildImageId(TechnicalDocumentationImageInput image)
    {
        return ClassifiedTechnicalDocumentationImage.BuildImageId(image);
    }

    private static bool IsSameImage(TechnicalDocumentationImageInput left, TechnicalDocumentationImageInput right)
    {
        return string.Equals(left.FileName, right.FileName, StringComparison.OrdinalIgnoreCase)
            && left.PageNumber == right.PageNumber;
    }

    private static int ResolveGroupOrder(string groupName)
    {
        return groupName switch
        {
            TechnicalDocumentationOptions.ThematicGroups.Site => 0,
            TechnicalDocumentationOptions.ThematicGroups.FloorPlans => 1,
            TechnicalDocumentationOptions.ThematicGroups.Sections => 2,
            TechnicalDocumentationOptions.ThematicGroups.Elevations => 3,
            TechnicalDocumentationOptions.ThematicGroups.Foundations => 4,
            TechnicalDocumentationOptions.ThematicGroups.Reinforcement => 5,
            TechnicalDocumentationOptions.ThematicGroups.RoofStructure => 6,
            _ => 99,
        };
    }
}
