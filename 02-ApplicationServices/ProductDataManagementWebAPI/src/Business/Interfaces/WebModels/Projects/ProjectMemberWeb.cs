using Entities.Enums;

namespace Business.Interfaces.WebModels.Projects
{
    public record ProjectMemberWeb(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        ProjectRole Role,
        DateTime JoinedAt
    );
}
