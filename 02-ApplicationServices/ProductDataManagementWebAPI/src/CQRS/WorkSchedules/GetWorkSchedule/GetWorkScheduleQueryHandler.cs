using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    public class GetWorkScheduleQueryHandler : IRequestHandler<GetWorkScheduleQuery, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IUserService userService;
        private readonly ICurrentUser currentUser;

        public GetWorkScheduleQueryHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IUserService userService,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.userService = userService;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(GetWorkScheduleQuery request, CancellationToken cancellationToken)
        {
            // ─────────────────────────────────────────────────────────────────────
            // STEP 1: Load work schedule with all related data
            // ─────────────────────────────────────────────────────────────────────
            if (request.TenantId != currentUser.ActiveTenantId)
                throw new ForbiddenApiException("Access to this tenant is not allowed.");

            var workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId &&
                      ws.ProjectId == request.ProjectId &&
                      ws.TenantId == request.TenantId &&
                      !ws.IsDeleted,
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Periods),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Assignments),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Comments))
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            // ─────────────────────────────────────────────────────────────────────
            // STEP 2: Determine if user can access this work schedule
            // ─────────────────────────────────────────────────────────────────────

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isOwner = workSchedule.CreatedByUserId == currentUser.Id;

            // Access rules:
            // - SuperAdmin → can view all work schedules
            // - Tenant Admin → can view all work schedules in their tenant
            // - Project Admin → can view all work schedules in their project
            // - Owner → can view their own work schedules
            bool canAccess = currentUser.IsSuperAdmin || isAdmin || isOwner;

            if (!canAccess)
            {
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());
            }

            // ─────────────────────────────────────────────────────────────────────
            // STEP 3: Build response
            // ─────────────────────────────────────────────────────────────────────
            var membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            // Filter out soft-deleted stages and build a tree in memory
            var activeStages = workSchedule.Stages.Where(s => !s.IsDeleted).ToList();

            List<WorkScheduleStageWeb> BuildStageTree(Guid? parentId)
            {
                return activeStages
                    .Where(s => s.ParentStageId == parentId)
                    .OrderBy(s => s.Order)
                    .Select(s => new WorkScheduleStageWeb(
                        Id: s.Id,
                        Name: s.Name,
                        Order: s.Order,
                        ParentStageId: s.ParentStageId,
                        CostEstimateGroupId: s.CostEstimateGroupId,
                        Works: s.Works
                            .OrderBy(w => w.Order)
                            .Select(w => new WorkScheduleStageWorkWeb(
                                Id: w.Id,
                                Name: w.Name,
                                Order: w.Order,
                                ColorRgb: w.ColorRgb,
                                IsClosed: w.IsClosed,
                                Periods: w.Periods
                                    .OrderBy(p => p.StartDate)
                                    .Select(p => new WorkScheduleStageWorkPeriodWeb(
                                        StartDate: p.StartDate,
                                        EndDate: p.EndDate,
                                        IsClosed: p.IsClosed))
                                    .ToList(),
                                Assignees: w.Assignments
                                    .Select(a => new WorkScheduleStageWorkAssigneeWeb(
                                        UserId: a.UserId,
                                        UserName: membersDict.TryGetValue(a.UserId, out var assignee)
                                            ? assignee.FullName
                                            : "Unknown"))
                                    .ToList(),
                                Comments: w.Comments
                                    .OrderBy(c => c.CreatedAt)
                                    .Select(c => new WorkScheduleStageWorkCommentWeb(
                                        Id: c.Id,
                                        Content: c.Content,
                                        CreatedByUserId: c.CreatedByUserId,
                                        CreatedByUserName: membersDict.TryGetValue(c.CreatedByUserId, out var commenter)
                                            ? commenter.FullName
                                            : "Unknown",
                                        CreatedAt: c.CreatedAt))
                                    .ToList()))
                            .ToList(),
                        ChildStages: BuildStageTree(s.Id)))
                    .ToList();
            }

            var result = new WorkScheduleDetailsWeb(
                Id: workSchedule.Id,
                TenantId: workSchedule.TenantId,
                ProjectId: workSchedule.ProjectId,
                CostEstimateId: workSchedule.CostEstimateId,
                Name: workSchedule.Name,
                CreatedAt: workSchedule.CreatedAt,
                CreatedByUserId: workSchedule.CreatedByUserId,
                CreatedByUserName: membersDict.TryGetValue(workSchedule.CreatedByUserId, out var creator)
                    ? creator.FullName
                    : "Unknown",
                Stages: BuildStageTree(null));

            return result;
        }
    }
}
