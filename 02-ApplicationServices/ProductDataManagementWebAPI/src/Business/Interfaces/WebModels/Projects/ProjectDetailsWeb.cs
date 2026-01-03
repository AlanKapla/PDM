using Entities.Enums;

namespace Business.Interfaces.WebModels.Projects
{
    /// <summary>
    /// Project details with role code instead of enum
    /// </summary>
    public record ProjectDetailsWeb(
        Guid Id,
        Guid TenantId,
        string Name,
        bool IsActive,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName,
        string UserRoleCode,  // Changed from ProjectRole enum
        int MembersCount
    );
}
