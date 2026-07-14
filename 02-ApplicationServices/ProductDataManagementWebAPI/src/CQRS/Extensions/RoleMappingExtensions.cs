using Business.Interfaces.Constants;

namespace CQRS.Extensions;

public static class RoleCodeExtensions
{
    public static bool IsTenantAdmin(this string? roleCode)
    {
        return roleCode == RoleCodes.TenantAdmin;
    }
}
