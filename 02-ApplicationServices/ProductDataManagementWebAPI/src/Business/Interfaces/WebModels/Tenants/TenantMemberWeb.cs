using Entities.Enums;

namespace Business.Interfaces.WebModels.Tenants
{
    public record TenantMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        TenantRole Role,
        bool IsActive,
        DateTime JoinedAt
    );
}
