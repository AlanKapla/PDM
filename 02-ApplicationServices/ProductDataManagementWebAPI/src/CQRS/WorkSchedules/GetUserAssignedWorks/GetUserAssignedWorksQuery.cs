using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public sealed record GetUserAssignedWorksQuery : WorkScheduleRequestBase, IRequestQuery<List<UserAssignedWorksByTenantWeb>>
    {
        public override string PermissionCode => PermissionCodes.ProjectResourcesRead;
    }
}
