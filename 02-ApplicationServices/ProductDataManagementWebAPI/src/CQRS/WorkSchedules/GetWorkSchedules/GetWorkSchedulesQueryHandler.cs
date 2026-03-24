using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetWorkSchedules
{
    /// <summary>
    /// Handler to retrieve work schedules based on scope (All, Mine, Shared)
    /// </summary>
    public class GetWorkSchedulesQueryHandler : IRequestHandler<GetWorkSchedulesQuery, List<WorkScheduleSummaryWeb>>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IUserService userService;
        private readonly ICurrentUser currentUser;

        public GetWorkSchedulesQueryHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IUserService userService,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.userService = userService;
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
                    workSchedules = await workScheduleRepo.GetBySearch(
                        ws => ws.ProjectId == request.ProjectId &&
                              ws.TenantId == request.TenantId);
                    break;

                case ResourceScope.Mine:
                    workSchedules = await workScheduleRepo.GetBySearch(
                        ws => ws.ProjectId == request.ProjectId &&
                              ws.TenantId == request.TenantId &&
                              ws.CreatedByUserId == currentUser.Id);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

            var membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            var result = workSchedules
                .OrderByDescending(ws => ws.CreatedAt)
                .Select(ws => new WorkScheduleSummaryWeb(
                    Id: ws.Id,
                    Name: ws.Name,
                    CreatedAt: ws.CreatedAt,
                    CreatedByUserId: ws.CreatedByUserId,
                    CreatedByUserName: membersDict.TryGetValue(ws.CreatedByUserId, out var creator)
                        ? creator.FullName
                        : "Unknown"
                ))
                .ToList();

            return result;
        }
    }
}
