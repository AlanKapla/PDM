using Entities.Enums;

namespace Business.Interfaces.WebModels.Projects
{
    /// <summary>
    /// Project details with user's role and permissions
    /// </summary>
    public record ProjectDetailsWeb(
        Guid Id,
        Guid TenantId,
        string Name,
        bool IsActive,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName,
        string UserRoleCode,
        int MembersCount,
        HashSet<string> UserPermissions,  // User's permissions for this project
        ProjectCurrencyWeb? Currency
    );
}
