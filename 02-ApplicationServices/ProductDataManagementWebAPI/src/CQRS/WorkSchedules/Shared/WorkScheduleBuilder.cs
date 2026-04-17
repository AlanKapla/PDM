using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.Shared
{
    public sealed class WorkScheduleBuilder
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepo;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepo;
        private readonly IUserService userService;

        public WorkScheduleBuilder(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkPeriod> periodRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IUserService userService)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.periodRepo = periodRepo;
            this.assignmentRepo = assignmentRepo;
            this.commentRepo = commentRepo;
            this.dependencyRepo = dependencyRepo;
            this.userService = userService;
        }

        public async Task<WorkScheduleDetailsWeb> BuildAsync(
            Guid workScheduleId,
            Guid tenantId,
            Guid projectId,
            CancellationToken ct)
        {
            // Step 1: load schedule
            ScheduleRow? schedule = (await workScheduleRepo.SelectAsync(
                ws => ws.Id == workScheduleId
                   && ws.TenantId == tenantId
                   && ws.ProjectId == projectId
                   && !ws.IsDeleted,
                ws => new ScheduleRow(ws.Id, ws.TenantId, ws.ProjectId, ws.CostEstimateId, ws.Name, ws.CreatedAt, ws.CreatedByUserId),
                ct)).FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());

            // Step 2: load stages
            List<StageRow> stages = await stageRepo.SelectAsync(
                s => s.WorkScheduleId == workScheduleId
                  && s.TenantId == tenantId
                  && !s.IsDeleted,
                s => new StageRow(s.Id, s.Name, s.Order, s.ParentStageId, s.CostEstimateGroupId),
                ct);

            List<Guid> stageIds = stages.Select(s => s.Id).ToList();

            // Step 3: load works for all stages
            List<WorkRow> works = stageIds.Count > 0
                ? await workRepo.SelectAsync(
                    w => stageIds.Contains(w.WorkScheduleStageId),
                    w => new WorkRow(w.Id, w.WorkScheduleStageId, w.Name, w.Order, w.ColorRgb, w.PlannedStartDate, w.PlannedEndDate),
                    ct)
                : new List<WorkRow>();

            List<Guid> workIds = works.Select(w => w.Id).ToList();

            // Step 4: load periods, assignees, comments and dependencies sequentially
            // (EF Core DbContext is not thread-safe — concurrent awaits on the same context are not allowed)
            List<PeriodRow> periods = workIds.Count > 0
                ? await periodRepo.SelectAsync(
                    p => workIds.Contains(p.WorkScheduleStageWorkId),
                    p => new PeriodRow(p.Id, p.WorkScheduleStageWorkId, p.StartDate, p.EndDate, p.IsClosed),
                    ct)
                : new List<PeriodRow>();

            List<AssigneeRow> assignees = workIds.Count > 0
                ? await assignmentRepo.SelectAsync(
                    a => workIds.Contains(a.WorkScheduleStageWorkId),
                    a => new AssigneeRow(a.WorkScheduleStageWorkId, a.UserId),
                    ct)
                : new List<AssigneeRow>();

            List<CommentRow> comments = workIds.Count > 0
                ? await commentRepo.SelectAsync(
                    c => workIds.Contains(c.WorkScheduleStageWorkId),
                    c => new CommentRow(c.Id, c.WorkScheduleStageWorkId, c.Content, c.CreatedByUserId, c.CreatedAt),
                    ct)
                : new List<CommentRow>();

            List<DependencyRow> dependencies = await dependencyRepo.SelectAsync(
                d => d.WorkScheduleId == workScheduleId,
                d => new DependencyRow(d.Id, d.PredecessorWorkId, d.SuccessorWorkId, d.DependencyType, d.LagDays),
                ct);

            // Step 5: load user names
            Dictionary<Guid, string> membersDict = (await userService.GetProjectMembersAsync(tenantId, projectId, ct))
                .ToDictionary(m => m.UserId, m => m.FullName);

            // Step 6: group child collections by parent id
            Dictionary<Guid, List<PeriodRow>> periodsByWork = periods
                .GroupBy(p => p.WorkScheduleStageWorkId)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.StartDate).ToList());

            Dictionary<Guid, List<AssigneeRow>> assigneesByWork = assignees
                .GroupBy(a => a.WorkScheduleStageWorkId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CommentRow>> commentsByWork = comments
                .GroupBy(c => c.WorkScheduleStageWorkId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

            Dictionary<Guid, List<WorkRow>> worksByStage = works
                .GroupBy(w => w.WorkScheduleStageId)
                .ToDictionary(g => g.Key, g => g.OrderBy(w => w.Order).ToList());

            // Step 7: build stage web objects
            Dictionary<Guid, WorkScheduleStageWeb> stageWebById = stages.ToDictionary(
                s => s.Id,
                s => new WorkScheduleStageWeb(
                    Id: s.Id,
                    Name: s.Name,
                    Order: s.Order,
                    ParentStageId: s.ParentStageId,
                    CostEstimateGroupId: s.CostEstimateGroupId,
                    Works: worksByStage.TryGetValue(s.Id, out List<WorkRow>? stageWorks)
                        ? stageWorks.Select(w => MapWork(w, periodsByWork, assigneesByWork, commentsByWork, membersDict)).ToList()
                        : new List<WorkScheduleStageWorkWeb>(),
                    ChildStages: new List<WorkScheduleStageWeb>()));

            // Step 8: assemble parent → child hierarchy
            List<WorkScheduleStageWeb> rootStages = new();

            foreach (WorkScheduleStageWeb stageWeb in stageWebById.Values.OrderBy(s => s.Order))
            {
                if (stageWeb.ParentStageId.HasValue && stageWebById.TryGetValue(stageWeb.ParentStageId.Value, out WorkScheduleStageWeb? parent))
                    parent.ChildStages.Add(stageWeb);
                else
                    rootStages.Add(stageWeb);
            }

            return new WorkScheduleDetailsWeb(
                Id: schedule.Id,
                TenantId: schedule.TenantId,
                ProjectId: schedule.ProjectId,
                CostEstimateId: schedule.CostEstimateId,
                Name: schedule.Name,
                CreatedAt: schedule.CreatedAt,
                CreatedByUserId: schedule.CreatedByUserId,
                CreatedByUserName: membersDict.TryGetValue(schedule.CreatedByUserId, out string? creatorName)
                    ? creatorName
                    : "Unknown",
                Stages: rootStages,
                Dependencies: dependencies.Select(d => new WorkScheduleWorkDependencyWeb(
                    Id: d.Id,
                    PredecessorWorkId: d.PredecessorWorkId,
                    SuccessorWorkId: d.SuccessorWorkId,
                    DependencyType: d.DependencyType,
                    LagDays: d.LagDays)).ToList());
        }

        private static WorkScheduleStageWorkWeb MapWork(
            WorkRow w,
            Dictionary<Guid, List<PeriodRow>> periodsByWork,
            Dictionary<Guid, List<AssigneeRow>> assigneesByWork,
            Dictionary<Guid, List<CommentRow>> commentsByWork,
            Dictionary<Guid, string> membersDict)
        {
            List<PeriodRow> workPeriods = periodsByWork.TryGetValue(w.Id, out List<PeriodRow>? p) ? p : new();
            List<AssigneeRow> workAssignees = assigneesByWork.TryGetValue(w.Id, out List<AssigneeRow>? a) ? a : new();
            List<CommentRow> workComments = commentsByWork.TryGetValue(w.Id, out List<CommentRow>? c) ? c : new();

            return new WorkScheduleStageWorkWeb(
                Id: w.Id,
                Name: w.Name,
                Order: w.Order,
                ColorRgb: w.ColorRgb,
                IsClosed: workPeriods.Count > 0 && workPeriods.All(pr => pr.IsClosed),
                PlannedStartDate: w.PlannedStartDate,
                PlannedEndDate: w.PlannedEndDate,
                Periods: workPeriods.Select(pr => new WorkScheduleStageWorkPeriodWeb(
                    Id: pr.Id,
                    StartDate: pr.StartDate,
                    EndDate: pr.EndDate,
                    IsClosed: pr.IsClosed)).ToList(),
                Assignees: workAssignees.Select(as_ => new WorkScheduleStageWorkAssigneeWeb(
                    UserId: as_.UserId,
                    UserName: membersDict.TryGetValue(as_.UserId, out string? userName) ? userName : "Unknown")).ToList(),
                Comments: workComments.Select(cm => new WorkScheduleStageWorkCommentWeb(
                    Id: cm.Id,
                    Content: cm.Content,
                    CreatedByUserId: cm.CreatedByUserId,
                    CreatedByUserName: membersDict.TryGetValue(cm.CreatedByUserId, out string? commenterName) ? commenterName : "Unknown",
                    CreatedAt: cm.CreatedAt)).ToList());
        }

        private sealed record ScheduleRow(Guid Id, Guid TenantId, Guid ProjectId, Guid? CostEstimateId, string Name, DateTime CreatedAt, Guid CreatedByUserId);
        private sealed record StageRow(Guid Id, string Name, int Order, Guid? ParentStageId, Guid? CostEstimateGroupId);
        private sealed record WorkRow(Guid Id, Guid WorkScheduleStageId, string Name, int Order, string ColorRgb, DateTime? PlannedStartDate, DateTime? PlannedEndDate);
        private sealed record PeriodRow(Guid Id, Guid WorkScheduleStageWorkId, DateTime StartDate, DateTime EndDate, bool IsClosed);
        private sealed record AssigneeRow(Guid WorkScheduleStageWorkId, Guid UserId);
        private sealed record CommentRow(Guid Id, Guid WorkScheduleStageWorkId, string Content, Guid CreatedByUserId, DateTime CreatedAt);
        private sealed record DependencyRow(Guid Id, Guid PredecessorWorkId, Guid SuccessorWorkId, WorkDependencyType DependencyType, int LagDays);
    }
}
