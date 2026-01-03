using Business.Interfaces.WebModels.Roles;
using Entities.Enums;

namespace CQRS.Roles.GetAvailableRoles
{
    /// <summary>
    /// Query to get available roles for a specific scope (Tenant or Project)
    /// </summary>
    public record GetAvailableRolesQuery(
        RoleScope Scope
    ) : IRequestQuery<IEnumerable<RoleWeb>>;
}
