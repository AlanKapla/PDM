using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public sealed record GetUserAssignedWorksQuery : IRequestQuery<List<UserAssignedWorksByTenantWeb>>
    {
    }
}
