using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
        private readonly ICurrentUser currentUser;

        public GetWorkScheduleQueryHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(GetWorkScheduleQuery request, CancellationToken cancellationToken)
        {
            // ─────────────────────────────────────────────────────────────────────
            // STEP 1: Load work schedule with all related data
            // ─────────────────────────────────────────────────────────────────────
            var workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId &&
                      ws.ProjectId == request.ProjectId &&
                      ws.TenantId == request.TenantId,
                include => include
                    .Include(ws => ws.CreatedBy)
                        .ThenInclude(tm => tm.User)
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Periods),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Assignments)
                                .ThenInclude(a => a.ProjectMember)
                                    .ThenInclude(pm => pm.TenantMember)
                                        .ThenInclude(tm => tm.User),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Comments)
                                .ThenInclude(c => c.CreatedBy))
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
            var result = new WorkScheduleDetailsWeb(
                Id: workSchedule.Id,
                TenantId: workSchedule.TenantId,
                ProjectId: workSchedule.ProjectId,
                Name: workSchedule.Name,
                CreatedAt: workSchedule.CreatedAt,
                CreatedByUserId: workSchedule.CreatedByUserId,
                CreatedByUserName: workSchedule.CreatedBy?.User != null
                    ? $"{workSchedule.CreatedBy.User.FirstName} {workSchedule.CreatedBy.User.LastName}".Trim()
                    : "Unknown",
                Stages: workSchedule.Stages
                    .OrderBy(s => s.Order)
                    .Select(s => new WorkScheduleStageWeb(
                        Id: s.Id,
                        Name: s.Name,
                        Order: s.Order,
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
                                        IsClosed: p.IsClosed
                                    ))
                                    .ToList(),
                                Assignees: w.Assignments
                                    .Select(a => new WorkScheduleStageWorkAssigneeWeb(
                                        UserId: a.UserId,
                                        UserName: $"{a.ProjectMember.TenantMember.User.FirstName} {a.ProjectMember.TenantMember.User.LastName}".Trim()
                                    ))
                                    .ToList(),
                                Comments: w.Comments
                                    .OrderBy(c => c.CreatedAt)
                                    .Select(c => new WorkScheduleStageWorkCommentWeb(
                                        Id: c.Id,
                                        Content: c.Content,
                                        CreatedByUserId: c.CreatedByUserId,
                                        CreatedByUserName: $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}".Trim(),
                                        CreatedAt: c.CreatedAt
                                    ))
                                    .ToList()
                            ))
                            .ToList()
                    ))
                    .ToList()
            );

            return result;
        }
    }
}
