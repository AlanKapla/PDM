using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.GetProjectMembers
{
    public record GetProjectMembersQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<IEnumerable<ProjectMemberWeb>>;
}
