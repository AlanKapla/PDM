using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models.Tenants;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetWorkScheduleAssignableAssignees
{
    public sealed class GetWorkScheduleAssignableAssigneesQueryHandler
        : IRequestHandler<GetWorkScheduleAssignableAssigneesQuery, WorkScheduleAssignableAssigneesWeb>
    {
        private readonly IUserService userService;
        private readonly IReadRepository<Contractor> contractorRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IReadRepository<WorkScheduleStageWorkPeriod> periodRepo;

        public GetWorkScheduleAssignableAssigneesQueryHandler(
            IUserService userService,
            IReadRepository<Contractor> contractorRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IReadRepository<WorkScheduleStageWorkPeriod> periodRepo)
        {
            this.userService = userService;
            this.contractorRepo = contractorRepo;
            this.assignmentRepo = assignmentRepo;
            this.periodRepo = periodRepo;
        }

        public async Task<WorkScheduleAssignableAssigneesWeb> Handle(
            GetWorkScheduleAssignableAssigneesQuery request,
            CancellationToken cancellationToken)
        {
            List<ProjectMemberUserInfo> memberInfos = await userService.GetProjectMembersAsync(
                request.TenantId,
                request.ProjectId,
                cancellationToken);

            IEnumerable<Contractor> contractorEntities = await contractorRepo.GetBySearch(
                c => c.TenantId == request.TenantId && !c.IsDeleted);

            List<Contractor> contractorList = contractorEntities.ToList();

            HashSet<Guid> userIds = memberInfos.Select(m => m.UserId).ToHashSet();
            HashSet<Guid> contractorIds = contractorList.Select(c => c.Id).ToHashSet();

            Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>> assignmentsByUser =
                new Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>>();
            Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>> assignmentsByContractor =
                new Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>>();

            if (userIds.Count > 0 || contractorIds.Count > 0)
            {
                await LoadAssignmentsAsync(
                    request.TenantId,
                    userIds,
                    contractorIds,
                    assignmentsByUser,
                    assignmentsByContractor,
                    cancellationToken);
            }

            List<WorkScheduleAssignableMemberWeb> members = memberInfos
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName)
                .Select(m => new WorkScheduleAssignableMemberWeb(
                    UserId: m.UserId,
                    Email: m.Email,
                    FirstName: m.FirstName,
                    LastName: m.LastName,
                    CompanyName: m.CompanyName,
                    Assignments: assignmentsByUser.TryGetValue(m.UserId, out List<WorkScheduleAssigneeBusyPeriodWeb>? userAssignments)
                        ? userAssignments
                        : Array.Empty<WorkScheduleAssigneeBusyPeriodWeb>()))
                .ToList();

            List<WorkScheduleAssignableContractorWeb> contractors = contractorList
                .OrderBy(c => c.Name)
                .Select(c => new WorkScheduleAssignableContractorWeb(
                    Id: c.Id,
                    Name: c.Name,
                    Assignments: assignmentsByContractor.TryGetValue(c.Id, out List<WorkScheduleAssigneeBusyPeriodWeb>? contractorAssignments)
                        ? contractorAssignments
                        : Array.Empty<WorkScheduleAssigneeBusyPeriodWeb>()))
                .ToList();

            return new WorkScheduleAssignableAssigneesWeb(members, contractors);
        }

        private async Task LoadAssignmentsAsync(
            Guid tenantId,
            HashSet<Guid> userIds,
            HashSet<Guid> contractorIds,
            Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>> assignmentsByUser,
            Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>> assignmentsByContractor,
            CancellationToken cancellationToken)
        {
            List<AssignmentRow> assignmentRows = await assignmentRepo.SelectAsync(
                a => a.TenantId == tenantId
                     && !a.Work.Stage.WorkSchedule.IsDeleted
                     && a.Project.IsActive
                     && (
                         (a.UserId.HasValue && userIds.Contains(a.UserId.Value))
                         || (a.ContractorId.HasValue && contractorIds.Contains(a.ContractorId.Value))
                     ),
                a => new AssignmentRow(
                    a.UserId,
                    a.ContractorId,
                    a.WorkScheduleStageWorkId,
                    a.Work.Name,
                    a.Work.Stage.WorkScheduleId,
                    a.Work.Stage.WorkSchedule.Name,
                    a.ProjectId,
                    a.Project.Name),
                cancellationToken);

            if (assignmentRows.Count == 0)
            {
                return;
            }

            HashSet<Guid> workIds = assignmentRows.Select(r => r.WorkId).ToHashSet();
            List<PeriodRow> periodRows = await periodRepo.SelectAsync(
                p => workIds.Contains(p.WorkScheduleStageWorkId) && !p.IsClosed,
                p => new PeriodRow(p.WorkScheduleStageWorkId, p.StartDate, p.EndDate),
                cancellationToken);

            Dictionary<Guid, List<PeriodRow>> periodsByWork = periodRows
                .GroupBy(p => p.WorkId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (AssignmentRow row in assignmentRows)
            {
                if (!periodsByWork.TryGetValue(row.WorkId, out List<PeriodRow>? periods))
                {
                    continue;
                }

                List<WorkScheduleAssigneeBusyPeriodWeb> busyPeriods = periods
                    .Select(p => new WorkScheduleAssigneeBusyPeriodWeb(
                        WorkId: row.WorkId,
                        WorkName: row.WorkName,
                        WorkScheduleId: row.WorkScheduleId,
                        WorkScheduleName: row.WorkScheduleName,
                        ProjectId: row.ProjectId,
                        ProjectName: row.ProjectName,
                        StartDate: p.StartDate,
                        EndDate: p.EndDate))
                    .ToList();

                if (row.UserId.HasValue)
                {
                    AppendBusy(assignmentsByUser, row.UserId.Value, busyPeriods);
                }
                else if (row.ContractorId.HasValue)
                {
                    AppendBusy(assignmentsByContractor, row.ContractorId.Value, busyPeriods);
                }
            }
        }

        private static void AppendBusy(
            Dictionary<Guid, List<WorkScheduleAssigneeBusyPeriodWeb>> target,
            Guid key,
            List<WorkScheduleAssigneeBusyPeriodWeb> busyPeriods)
        {
            if (!target.TryGetValue(key, out List<WorkScheduleAssigneeBusyPeriodWeb>? list))
            {
                list = new List<WorkScheduleAssigneeBusyPeriodWeb>();
                target[key] = list;
            }

            list.AddRange(busyPeriods);
        }

        private sealed record AssignmentRow(
            Guid? UserId,
            Guid? ContractorId,
            Guid WorkId,
            string WorkName,
            Guid WorkScheduleId,
            string WorkScheduleName,
            Guid ProjectId,
            string ProjectName);

        private sealed record PeriodRow(Guid WorkId, DateTime StartDate, DateTime EndDate);
    }
}
