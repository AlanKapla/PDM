namespace Chat.DTOs;

/// <summary>
/// API-facing DTO grouping project mates by project and tenant context.
/// </summary>
public record ProjectContactsGroupWeb(
    Guid ProjectId,
    string ProjectName,
    Guid TenantId,
    string TenantName,
    List<ProjectMateWeb> Members);
