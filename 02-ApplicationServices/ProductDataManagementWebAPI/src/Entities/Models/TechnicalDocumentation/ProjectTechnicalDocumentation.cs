using Entities.Enums;
using Entities.Models.Base;
using Entities.Models.Projects;

namespace Entities.Models.TechnicalDocumentation;

public class ProjectTechnicalDocumentation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public TechnicalDocumentationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DetailsJson { get; set; }
    public int AutoRetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Project Project { get; set; } = default!;
    public virtual ICollection<ProjectTechnicalDocumentationFile> Files { get; set; } =
        new List<ProjectTechnicalDocumentationFile>();
}
