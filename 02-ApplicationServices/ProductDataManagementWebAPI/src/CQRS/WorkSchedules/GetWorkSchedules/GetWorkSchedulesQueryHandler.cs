using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using Entities.Models.WorkItemLinks;
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
        private readonly IReadRepository<CostEstimateWorkScheduleLink> workScheduleLinkRepository;
        private readonly IUserService userService;
        private readonly ICurrentUser currentUser;

        public GetWorkSchedulesQueryHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IReadRepository<CostEstimateWorkScheduleLink> workScheduleLinkRepository,
            IUserService userService,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.workScheduleLinkRepository = workScheduleLinkRepository;
            this.userService = userService;
            this.currentUser = currentUser;
        }

        public async Task<List<WorkScheduleSummaryWeb>> Handle(GetWorkSchedulesQuery request, CancellationToken cancellationToken)
        {
            // Shared work schedules are not implemented yet
            if (request.Scope == ResourceScope.Shared)
            {
                return new List<WorkScheduleSummaryWeb>();
            }

            IEnumerable<WorkSchedule> workSchedules;

            switch (request.Scope)
            {
                case ResourceScope.All:
                    workSchedules = await workScheduleRepo.GetBySearch(
                        ws => ws.ProjectId == request.ProjectId &&
                              ws.TenantId == request.TenantId &&
                              !ws.IsDeleted);
                    break;

                case ResourceScope.Mine:
                    workSchedules = await workScheduleRepo.GetBySearch(
                        ws => ws.ProjectId == request.ProjectId &&
                              ws.TenantId == request.TenantId &&
                              ws.CreatedByUserId == currentUser.Id &&
                              !ws.IsDeleted);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

            var workScheduleIds = workSchedules.Select(ws => ws.Id).ToHashSet();

            var costEstimateIdByWorkScheduleId = (await workScheduleLinkRepository.GetBySearch(
                    l => l.WorkScheduleId != null && workScheduleIds.Contains(l.WorkScheduleId!.Value)))
                .GroupBy(l => l.WorkScheduleId!.Value)
                .ToDictionary(g => g.Key, g => g.First().CostEstimateId);

            var membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            var result = workSchedules
                .OrderByDescending(ws => ws.CreatedAt)
                .Select(ws => new WorkScheduleSummaryWeb(
                    Id: ws.Id,
                    CostEstimateId: costEstimateIdByWorkScheduleId.TryGetValue(ws.Id, out var ceId) ? ceId : null,
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
