using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using MediatR;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    public sealed class GetWorkScheduleQueryHandler : IRequestHandler<GetWorkScheduleQuery, WorkScheduleDetailsWeb>
    {
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly WorkScheduleBuilder scheduleBuilder;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleAccessService accessService;

        public GetWorkScheduleQueryHandler(
            IWorkScheduleCacheService scheduleCache,
            WorkScheduleBuilder scheduleBuilder,
            ICurrentUser currentUser,
            IWorkScheduleAccessService accessService)
        {
            this.scheduleCache = scheduleCache;
            this.scheduleBuilder = scheduleBuilder;
            this.currentUser = currentUser;
            this.accessService = accessService;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(GetWorkScheduleQuery request, CancellationToken cancellationToken)
        {
            if (request.TenantId != currentUser.ActiveTenantId)
            {
                throw new ForbiddenApiException("Access to this tenant is not allowed.");
            }

            WorkScheduleDetailsWeb? result = await scheduleCache.GetOrBuildScheduleAsync(
                request.WorkScheduleId,
                () => scheduleBuilder.BuildAsync(request.WorkScheduleId, request.TenantId, request.ProjectId, cancellationToken),
                cancellationToken);

            if (result is null)
            {
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());
            }

            // Access rules: SuperAdmin / Tenant or Project Admin / Owner.
            // Delegate the admin-or-owner check to IWorkScheduleAccessService and rethrow
            // Forbidden as NotFound to preserve the existing security-through-obscurity contract.
            if (!currentUser.IsSuperAdmin)
            {
                try
                {
                    await accessService.RequireAdminOrOwnerAsync(
                        request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);
                }
                catch (ForbiddenApiException)
                {
                    throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());
                }
            }

            return result;
        }
    }
}
