namespace Business.Interfaces.WebModels.Chats;

/// <summary>
/// API-facing DTO grouping project mates by project and tenant context.
/// </summary>
public sealed record ProjectContactsGroupWeb(
    Guid ProjectId,
    string ProjectName,
    Guid TenantId,
    string TenantName,
    List<ProjectMateWeb> Members);
