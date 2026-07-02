using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Interfaces.Services.TechnicalDocumentation;

public sealed class ClassifiedTechnicalDocumentationImage
{
    public required TechnicalDocumentationImageInput Image { get; init; }

    public required DrawingClassification Classification { get; init; }

    public string ImageId => BuildImageId(Image);

    public static string BuildImageId(TechnicalDocumentationImageInput image)
    {
        return $"{image.FileName}::{image.PageNumber}";
    }
}

public sealed class ThematicDrawingGroup
{
    public required string GroupName { get; init; }

    public required IReadOnlyList<ClassifiedTechnicalDocumentationImage> Images { get; init; }
}

public sealed class GroupExtractionPairResult
{
    public required string GroupName { get; init; }

    public required string ResultAJson { get; init; }

    public required string ResultBJson { get; init; }
}

public sealed class VerifiedGroupExtractionResult
{
    public required string GroupName { get; init; }

    public required string VerifiedJson { get; init; }

    public List<string> Warnings { get; } = new();

    public bool HadCriticalDiff { get; init; }

    public bool AgentCInvoked { get; init; }
}
