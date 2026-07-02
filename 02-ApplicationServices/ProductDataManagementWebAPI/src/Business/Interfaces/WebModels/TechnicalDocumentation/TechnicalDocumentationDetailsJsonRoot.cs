using Business.Interfaces.WebModels.TechnicalDocumentation.Models;

namespace Business.Interfaces.WebModels.TechnicalDocumentation;

/// <summary>
/// Root DTO for <c>DetailsJson</c> (spec §8.1): projectModel + materialSchedule + auditResult.
/// </summary>
public sealed class TechnicalDocumentationDetailsJsonRoot
{
    public ProjectModel? ProjectModel { get; set; }
    public DetailsMaterialSchedule? MaterialSchedule { get; set; }
    public AuditResult? AuditResult { get; set; }
    public int TokenUsage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
