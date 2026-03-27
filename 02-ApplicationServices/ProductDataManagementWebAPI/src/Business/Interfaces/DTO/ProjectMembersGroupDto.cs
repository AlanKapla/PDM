namespace Business.Interfaces.DTO;

/// <summary>
/// Internal model representing a project and the IDs of its members (excluding the requesting user).
/// Used to build grouped contact lists.
/// </summary>
public record ProjectMembersGroupDto(
    Guid ProjectId,
    string ProjectName,
    Guid TenantId,
    string TenantName,
    List<Guid> MemberUserIds);
