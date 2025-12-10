using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public class GetUserAssignedWorksQueryHandler : IRequestHandler<GetUserAssignedWorksQuery, List<UserAssignedWorksGroupedWeb>>
    {
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly ICurrentUser currentUser;

        public GetUserAssignedWorksQueryHandler(
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            ICurrentUser currentUser)
        {
            this.assignmentRepo = assignmentRepo;
            this.currentUser = currentUser;
        }

        public async Task<List<UserAssignedWorksGroupedWeb>> Handle(GetUserAssignedWorksQuery request, CancellationToken cancellationToken)
        {
            // Get all assignments for the current user in the active tenant
            var assignments = await assignmentRepo.GetBySearch(
                a => a.TenantId == request.TenantId && a.UserId == currentUser.Id,
                include => include
                    .Include(a => a.Work)
                        .ThenInclude(w => w.Periods)
                    .Include(a => a.Work)
                        .ThenInclude(w => w.Stage)
                            .ThenInclude(s => s.WorkSchedule)
                                .ThenInclude(ws => ws.Project));

            // Group by Project > WorkSchedule > Stage > Work
            var groupedByProject = assignments
                .GroupBy(a => new
                {
                    ProjectId = a.Work.Stage.WorkSchedule.ProjectId,
                    ProjectName = a.Work.Stage.WorkSchedule.Project.Name
                })
                .Select(projectGroup => new UserAssignedWorksGroupedWeb(
                    ProjectId: projectGroup.Key.ProjectId,
                    ProjectName: projectGroup.Key.ProjectName,
                    WorkSchedules: projectGroup
                        .GroupBy(a => new
                        {
                            WorkScheduleId = a.Work.Stage.WorkScheduleId,
                            WorkScheduleName = a.Work.Stage.WorkSchedule.Name,
                            WorkScheduleCreatedAt = a.Work.Stage.WorkSchedule.CreatedAt
                        })
                        .Select(wsGroup => new UserAssignedWorkScheduleWeb(
                            WorkScheduleId: wsGroup.Key.WorkScheduleId,
                            WorkScheduleName: wsGroup.Key.WorkScheduleName,
                            WorkScheduleCreatedAt: wsGroup.Key.WorkScheduleCreatedAt,
                            Stages: wsGroup
                                .GroupBy(a => new
                                {
                                    StageId = a.Work.WorkScheduleStageId,
                                    StageName = a.Work.Stage.Name,
                                    StageOrder = a.Work.Stage.Order
                                })
                                .Select(stageGroup => new UserAssignedStageWeb(
                                    StageId: stageGroup.Key.StageId,
                                    StageName: stageGroup.Key.StageName,
                                    StageOrder: stageGroup.Key.StageOrder,
                                    Works: stageGroup
                                        .Select(a => new UserAssignedWorkWeb(
                                            WorkId: a.WorkScheduleStageWorkId,
                                            WorkName: a.Work.Name,
                                            WorkOrder: a.Work.Order,
                                            ColorRgb: a.Work.ColorRgb,
                                            IsClosed: a.Work.IsClosed,
                                            Periods: a.Work.Periods
                                                .OrderBy(p => p.StartDate)
                                                .Select(p => new WorkScheduleStageWorkPeriodWeb(
                                                    StartDate: p.StartDate,
                                                    EndDate: p.EndDate
                                                ))
                                                .ToList()
                                        ))
                                        .OrderBy(w => w.WorkOrder)
                                        .ToList()
                                ))
                                .OrderBy(s => s.StageOrder)
                                .ToList()
                        ))
                        .OrderByDescending(ws => ws.WorkScheduleCreatedAt)
                        .ToList()
                ))
                .OrderBy(p => p.ProjectName)
                .ToList();

            return groupedByProject;
        }
    }
}
