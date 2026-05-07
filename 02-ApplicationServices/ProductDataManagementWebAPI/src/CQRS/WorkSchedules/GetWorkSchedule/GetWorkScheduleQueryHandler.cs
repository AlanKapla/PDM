using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    public class GetWorkScheduleQueryHandler : IRequestHandler<GetWorkScheduleQuery, WorkScheduleDetailsWeb>
    {
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly WorkScheduleBuilder scheduleBuilder;
        private readonly ICurrentUser currentUser;

        public GetWorkScheduleQueryHandler(
            IWorkScheduleCacheService scheduleCache,
            WorkScheduleBuilder scheduleBuilder,
            ICurrentUser currentUser)
        {
            this.scheduleCache = scheduleCache;
            this.scheduleBuilder = scheduleBuilder;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(GetWorkScheduleQuery request, CancellationToken cancellationToken)
        {
            if (request.TenantId != currentUser.ActiveTenantId)
                throw new ForbiddenApiException("Access to this tenant is not allowed.");

            WorkScheduleDetailsWeb? result = await scheduleCache.GetOrBuildScheduleAsync(
                request.WorkScheduleId,
                () => scheduleBuilder.BuildAsync(request.WorkScheduleId, request.TenantId, request.ProjectId, cancellationToken),
                cancellationToken);

            if (result is null)
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            // Access rules:
            // - SuperAdmin → can view all work schedules
            // - Tenant Admin → can view all work schedules in their tenant
            // - Project Admin → can view all work schedules in their project
            // - Owner → can view their own work schedules
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isOwner = result.CreatedByUserId == currentUser.Id;
            bool canAccess = currentUser.IsSuperAdmin || isAdmin || isOwner;

            if (!canAccess)
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            return result;
        }
    }
}
