using Entities.Enums;

namespace Business.Interfaces.WebModels.Projects
{
    public record ProjectDetailsWeb(
        Guid Id,
        Guid TenantId,
        string Name,
        bool IsActive,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName,
        ProjectRole UserRole,
        int MembersCount
    );
}