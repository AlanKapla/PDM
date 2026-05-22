using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public sealed class GetUserAssignedWorksQueryHandler : IRequestHandler<GetUserAssignedWorksQuery, List<UserAssignedWorksByTenantWeb>>
    {
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepo;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepo;
        private readonly ICurrentUser currentUser;

        public GetUserAssignedWorksQueryHandler(
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkPeriod> periodRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            ICurrentUser currentUser)
        {
            this.assignmentRepo = assignmentRepo;
            this.periodRepo = periodRepo;
            this.commentRepo = commentRepo;
            this.currentUser = currentUser;
        }

        public async Task<List<UserAssignedWorksByTenantWeb>> Handle(GetUserAssignedWorksQuery request, CancellationToken cancellationToken)
        {
            // Query 1: flat projection across all navigation properties — EF translates to JOINs, no Include needed
            List<AssignmentRow> assignmentRows = await assignmentRepo.SelectAsync(
                a => a.UserId == currentUser.Id
                     && a.Tenant.IsActive
                     && a.Project.IsActive
                     && !a.Work.Stage.WorkSchedule.IsDeleted
                     && a.TenantMember.UserId == currentUser.Id
                     && a.ProjectMember.UserId == currentUser.Id,
                a => new AssignmentRow(
                    a.TenantId,
                    a.Tenant.Name,
                    a.ProjectId,
                    a.Project.Name,
                    a.Work.Stage.WorkScheduleId,
                    a.Work.Stage.WorkSchedule.Name,
                    a.Work.Stage.WorkSchedule.CreatedAt,
                    a.Work.WorkScheduleStageId,
                    a.Work.Stage.Name,
                    a.Work.Stage.Order,
                    a.WorkScheduleStageWorkId,
                    a.Work.Name,
                    a.Work.Order,
                    a.Work.ColorRgb),
                cancellationToken);

            if (assignmentRows.Count == 0)
                return new List<UserAssignedWorksByTenantWeb>();

            HashSet<Guid> workIds = assignmentRows.Select(r => r.WorkId).ToHashSet();

            // Query 2: periods for the assigned works only
            List<PeriodRow> periodRows = await periodRepo.SelectAsync(
                p => workIds.Contains(p.WorkScheduleStageWorkId),
                p => new PeriodRow(p.Id, p.WorkScheduleStageWorkId, p.StartDate, p.EndDate, p.IsClosed),
                cancellationToken);

            // Query 3: comments for the assigned works only
            List<CommentRow> commentRows = await commentRepo.SelectAsync(
                c => workIds.Contains(c.WorkScheduleStageWorkId),
                c => new CommentRow(
                    c.Id,
                    c.WorkScheduleStageWorkId,
                    c.Content,
                    c.CreatedByUserId,
                    c.CreatedBy.FirstName,
                    c.CreatedBy.LastName,
                    c.CreatedAt),
                cancellationToken);

            // Build lookup dictionaries
            Dictionary<Guid, List<PeriodRow>> periodsByWork = periodRows
                .GroupBy(p => p.WorkId)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.StartDate).ToList());

            Dictionary<Guid, List<CommentRow>> commentsByWork = commentRows
                .GroupBy(c => c.WorkId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

            // Assemble result hierarchy in-memory
            return assignmentRows
                .GroupBy(r => new { r.TenantId, r.TenantName })
                .Select(tenantGroup => new UserAssignedWorksByTenantWeb(
                    TenantId: tenantGroup.Key.TenantId,
                    TenantName: tenantGroup.Key.TenantName,
                    Projects: tenantGroup
                        .GroupBy(r => new { r.ProjectId, r.ProjectName })
                        .Select(projectGroup => new UserAssignedWorksGroupedWeb(
                            ProjectId: projectGroup.Key.ProjectId,
                            ProjectName: projectGroup.Key.ProjectName,
                            WorkSchedules: projectGroup
                                .GroupBy(r => new { r.WorkScheduleId, r.WorkScheduleName, r.WorkScheduleCreatedAt })
                                .Select(wsGroup => new UserAssignedWorkScheduleWeb(
                                    WorkScheduleId: wsGroup.Key.WorkScheduleId,
                                    WorkScheduleName: wsGroup.Key.WorkScheduleName,
                                    WorkScheduleCreatedAt: wsGroup.Key.WorkScheduleCreatedAt,
                                    Stages: wsGroup
                                        .GroupBy(r => new { r.StageId, r.StageName, r.StageOrder })
                                        .Select(stageGroup => new UserAssignedStageWeb(
                                            StageId: stageGroup.Key.StageId,
                                            StageName: stageGroup.Key.StageName,
                                            StageOrder: stageGroup.Key.StageOrder,
                                            Works: stageGroup
                                                .Select(r => MapWork(r, periodsByWork, commentsByWork))
                                                .OrderBy(w => w.WorkOrder)
                                                .ToList()))
                                        .OrderBy(s => s.StageOrder)
                                        .ToList()))
                                .OrderByDescending(ws => ws.WorkScheduleCreatedAt)
                                .ToList()))
                        .OrderBy(p => p.ProjectName)
                        .ToList()))
                .OrderBy(t => t.TenantName)
                .ToList();
        }

        private static UserAssignedWorkWeb MapWork(
            AssignmentRow r,
            Dictionary<Guid, List<PeriodRow>> periodsByWork,
            Dictionary<Guid, List<CommentRow>> commentsByWork)
        {
            List<PeriodRow> periods = periodsByWork.TryGetValue(r.WorkId, out List<PeriodRow>? p) ? p : new();
            List<CommentRow> comments = commentsByWork.TryGetValue(r.WorkId, out List<CommentRow>? c) ? c : new();

            return new UserAssignedWorkWeb(
                WorkId: r.WorkId,
                WorkName: r.WorkName,
                WorkOrder: r.WorkOrder,
                ColorRgb: r.ColorRgb,
                IsClosed: periods.Count > 0 && periods.All(pr => pr.IsClosed),
                Periods: periods
                    .Select(pr => new WorkScheduleStageWorkPeriodWeb(
                        Id: pr.Id,
                        StartDate: pr.StartDate,
                        EndDate: pr.EndDate,
                        IsClosed: pr.IsClosed))
                    .ToList(),
                Comments: comments
                    .Select(cm => new WorkScheduleStageWorkCommentWeb(
                        Id: cm.Id,
                        Content: cm.Content,
                        CreatedByUserId: cm.CreatedByUserId,
                        CreatedByUserName: $"{cm.FirstName} {cm.LastName}".Trim(),
                        CreatedAt: cm.CreatedAt))
                    .ToList());
        }

        private sealed record AssignmentRow(
            Guid TenantId,
            string TenantName,
            Guid ProjectId,
            string ProjectName,
            Guid WorkScheduleId,
            string WorkScheduleName,
            DateTime WorkScheduleCreatedAt,
            Guid StageId,
            string StageName,
            int StageOrder,
            Guid WorkId,
            string WorkName,
            int WorkOrder,
            string ColorRgb);

        private sealed record PeriodRow(
            Guid Id,
            Guid WorkId,
            DateTime StartDate,
            DateTime EndDate,
            bool IsClosed);

        private sealed record CommentRow(
            Guid Id,
            Guid WorkId,
            string Content,
            Guid CreatedByUserId,
            string FirstName,
            string LastName,
            DateTime CreatedAt);
    }
}
