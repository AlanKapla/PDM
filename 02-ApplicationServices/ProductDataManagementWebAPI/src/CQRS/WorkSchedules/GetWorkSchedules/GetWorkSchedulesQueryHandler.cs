using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetWorkSchedules
{
    /// <summary>
    /// Handler to retrieve work schedules based on scope (All, Mine, Shared)
    /// </summary>
    public class GetWorkSchedulesQueryHandler : IRequestHandler<GetWorkSchedulesQuery, List<WorkScheduleSummaryWeb>>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetWorkSchedulesQueryHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<List<WorkScheduleSummaryWeb>> Handle(GetWorkSchedulesQuery request, CancellationToken cancellationToken)
        {
            // Shared work schedules are not implemented yet
            if (request.Scope == ResourceScope.Shared)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, "Shared work schedules are not yet supported");
            }

            IEnumerable<WorkSchedule> workSchedules;

            switch (request.Scope)
            {
                case ResourceScope.All:
                    // Get all work schedules in the project (requires READ_ALL permission)
                    workSchedules = await workScheduleRepo.GetBySearch(
                        ws => ws.ProjectId == request.ProjectId &&
                              ws.TenantId == request.TenantId,
                        include => include
                            .Include(ws => ws.CreatedBy)
                                .ThenInclude(tm => tm.User));
                    break;

                case ResourceScope.Mine:
                    // Get only work schedules created by the current user (requires READ permission)
                    workSchedules = await workScheduleRepo.GetBySearch(
                        ws => ws.ProjectId == request.ProjectId &&
                              ws.TenantId == request.TenantId &&
                              ws.CreatedByUserId == currentUser.Id,
                        include => include
                            .Include(ws => ws.CreatedBy)
                                .ThenInclude(tm => tm.User));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

            var result = workSchedules
                .OrderByDescending(ws => ws.CreatedAt)
                .Select(ws => new WorkScheduleSummaryWeb(
                    Id: ws.Id,
                    Name: ws.Name,
                    CreatedAt: ws.CreatedAt,
                    CreatedByUserId: ws.CreatedByUserId,
                    CreatedByUserName: $"{ws.CreatedBy.User.FirstName} {ws.CreatedBy.User.LastName}".Trim()
                ))
                .ToList();

            return result;
        }
    }
}
