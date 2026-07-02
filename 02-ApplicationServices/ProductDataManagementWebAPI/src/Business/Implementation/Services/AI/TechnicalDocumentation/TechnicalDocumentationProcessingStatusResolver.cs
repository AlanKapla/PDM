using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Entities.Enums;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationProcessingStatusResolver
{
    public static TechnicalDocumentationStatus Resolve(ProjectTechnicalDocumentationDetails details)
    {
        if (HasBlockingIssues(details))
        {
            return TechnicalDocumentationStatus.CompletedWithWarnings;
        }

        return TechnicalDocumentationStatus.Completed;
    }

    private static bool HasBlockingIssues(ProjectTechnicalDocumentationDetails details)
    {
        if (details.AuditResult?.Warnings.Count > 0)
        {
            return true;
        }

        if (details.ProjectModel?.Warnings.Count > 0)
        {
            return true;
        }

        if (details.ProjectModel?.Conflicts.Count > 0)
        {
            return true;
        }

        if (details.MaterialSchedule is not null && details.ValidationSummaries.Count > 0)
        {
            return true;
        }

        return false;
    }
}
