using Entities.Enums;

namespace Business.Interfaces.WebModels.Roles
{
    /// <summary>
    /// Web model representing a role available in the system
    /// </summary>
    public record RoleWeb(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        RoleScope Scope
    );
}
