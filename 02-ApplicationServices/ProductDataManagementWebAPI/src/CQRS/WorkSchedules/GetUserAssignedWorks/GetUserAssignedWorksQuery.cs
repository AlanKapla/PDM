using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public record GetUserAssignedWorksQuery(
        Guid TenantId
    ) : IRequestQuery<List<UserAssignedWorksGroupedWeb>>;
}
