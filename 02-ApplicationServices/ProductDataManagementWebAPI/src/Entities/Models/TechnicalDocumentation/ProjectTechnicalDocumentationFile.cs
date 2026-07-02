using Entities.Models.Base;

namespace Entities.Models.TechnicalDocumentation;

public class ProjectTechnicalDocumentationFile : BaseEntity
{
    public Guid TechnicalDocumentationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string BlobName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ProjectTechnicalDocumentation TechnicalDocumentation { get; set; } = default!;
}
