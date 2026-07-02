using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using Business.Interfaces.WebModels.TechnicalDocumentation.Validation;

namespace Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;

internal static class TechnicalDocumentationPipelineHelpers
{
    public static List<string> CollectFailedPages(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        TechnicalDocumentationPartialResult?[] results)
    {
        List<string> failedPages = new();

        for (int index = 0; index < images.Count; index++)
        {
            if (results[index] is null)
            {
                TechnicalDocumentationImageInput image = images[index];
                failedPages.Add($"{image.FileName} (str. {image.PageNumber})");
            }
        }

        return failedPages;
    }

    public static void AppendFailedPageWarnings(
        ProjectTechnicalDocumentationDetails details,
        List<string> failedPages)
    {
        if (failedPages.Count == 0)
        {
            return;
        }

        details.AuditResult ??= new AuditResult();
        details.AuditResult.Warnings.Add(
            $"Nie przetworzono {failedPages.Count} stron: {string.Join(", ", failedPages)}");
    }

    public static List<DrawingValidationSummary> BuildValidationSummaries(
        List<TechnicalDocumentationPartialResult> results)
    {
        List<DrawingValidationSummary> summaries = new();

        foreach (TechnicalDocumentationPartialResult result in results)
        {
            ValidationReport? report = result.Drawing.ValidationReport;
            summaries.Add(new DrawingValidationSummary
            {
                FileName = result.FileName,
                PageNumber = result.PageNumber,
                SheetNumber = result.Drawing.Classification.SheetNumber,
                DrawingType = result.Drawing.Classification.DrawingType,
                CrossValidationUsed = result.CrossValidationUsed,
                ConfidenceScore = ResolveConfidenceScore(report, result.CrossValidationUsed),
                Disagreements = report?.Disagreements
                    .Select(d => d.FieldPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToList() ?? []
            });
        }

        return summaries;
    }

    public static string ResolveBuildingType(ProjectModel projectModel)
    {
        string projectName = (projectModel.Project.Name ?? string.Empty).ToLowerInvariant();

        if (projectName.Contains("wielorodzin") || projectName.Contains("blok") || projectName.Contains("apartament"))
        {
            return "wielorodzinny";
        }

        return "jednorodzinny";
    }

    public static void ApplyAgentResult(
        TechnicalDocumentationAgentContext context,
        TechnicalDocumentationAgentResult result)
    {
        context.AgentExecutions.Add(result);

        foreach (string warning in result.Warnings)
        {
            if (string.IsNullOrWhiteSpace(warning))
            {
                continue;
            }

            if (!context.PipelineWarnings.Contains(warning))
            {
                context.PipelineWarnings.Add(warning);
            }
        }
    }

    private static string ResolveConfidenceScore(ValidationReport? report, bool crossValidationUsed)
    {
        if (!crossValidationUsed)
        {
            return "high";
        }

        if (report is null)
        {
            return "medium";
        }

        if (report.LowConfidence > 0)
        {
            return "low";
        }

        if (report.Disagreements.Count > 0)
        {
            return "medium";
        }

        return "high";
    }
}
