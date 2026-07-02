using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

public static class TechnicalDocumentationDetailsAggregator
{
    public static JoinerySummary AggregateJoineryForLegacy(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        ProjectTechnicalDocumentationDetails details = new();
        ProjectTechnicalDocumentationDetailsBuilder.Apply(
            details,
            new ProjectModel(),
            drawings,
            [],
            computedSchedule: null,
            buildingType: string.Empty);
        return details.Joinery ?? new JoinerySummary();
    }

    public static InstallationsSummary AggregateInstallationsForLegacy(IReadOnlyList<FloorPlanDrawing> drawings)
    {
        ProjectTechnicalDocumentationDetails details = new();
        ProjectTechnicalDocumentationDetailsBuilder.Apply(
            details,
            new ProjectModel(),
            drawings,
            [],
            computedSchedule: null,
            buildingType: string.Empty);
        return details.Installations;
    }

    public static ProjectTechnicalDocumentationDetails Aggregate(
        IReadOnlyList<FloorPlanDrawing> drawings,
        IReadOnlyList<DrawingDependencyLink>? dependencies = null)
    {
        ProjectTechnicalDocumentationDetails details = new();
        if (drawings.Count == 0)
        {
            return details;
        }

        ProjectTechnicalDocumentationDetailsBuilder.Apply(
            details,
            new ProjectModel(),
            drawings,
            dependencies ?? [],
            computedSchedule: null,
            buildingType: string.Empty);

        return details;
    }
}
