using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class WorkScheduleAccessService : IWorkScheduleAccessService
    {
        private readonly ICurrentUser currentUser;
        private readonly IReadRepository<WorkSchedule> workScheduleRepository;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepository;

        public WorkScheduleAccessService(
            ICurrentUser currentUser,
            IReadRepository<WorkSchedule> workScheduleRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository)
        {
            this.currentUser = currentUser;
            this.workScheduleRepository = workScheduleRepository;
            this.assignmentRepository = assignmentRepository;
        }

        public async Task RequireAdminOrOwnerAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);
            if (isAdmin)
            {
                return;
            }

            bool isOwner = await workScheduleRepository.AnyAsync(
                ws => ws.Id == workScheduleId
                   && ws.CreatedByUserId == currentUser.Id
                   && !ws.IsDeleted,
                cancellationToken);

            if (!isOwner)
            {
                throw new ForbiddenApiException("You must be an admin or the owner of this work schedule.");
            }
        }

        public async Task RequireAdminOwnerOrAssignedAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            Guid workScheduleStageWorkId,
            CancellationToken cancellationToken = default)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);
            if (isAdmin)
            {
                return;
            }

            bool isOwner = await workScheduleRepository.AnyAsync(
                ws => ws.Id == workScheduleId
                   && ws.CreatedByUserId == currentUser.Id
                   && !ws.IsDeleted,
                cancellationToken);

            if (isOwner)
            {
                return;
            }

            bool isAssigned = await assignmentRepository.AnyAsync(
                a => a.WorkScheduleStageWorkId == workScheduleStageWorkId
                  && a.UserId == currentUser.Id,
                cancellationToken);

            if (!isAssigned)
            {
                throw new ForbiddenApiException("You must be an admin, the owner of this work schedule, or assigned to this work item.");
            }
        }
    }
}
