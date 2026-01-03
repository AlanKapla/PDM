using Entities.Enums;

namespace Business.Interfaces.WebModels.Projects
{
    /// <summary>
    /// Project member details with role code instead of enum
    /// </summary>
    public record ProjectMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string RoleCode,  // Changed from ProjectRole enum to string
        DateTime JoinedAt
    );
}
