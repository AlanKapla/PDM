using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.Shared
{
    public static class WorkScheduleQueryHelper
    {
        public static string FormatFullName(string? firstName, string? lastName) =>
            $"{firstName} {lastName}".Trim();

        public static async Task<Dictionary<Guid, string>> BuildUserNameDictAsync(
            IReadRepository<User> userRepo,
            IEnumerable<WorkScheduleStageDto> stages,
            CancellationToken cancellationToken)
        {
            List<Guid> allUserIds = WorkScheduleValidationHelper.FlattenStages(stages)
                .Where(s => s.Works != null)
                .SelectMany(s => s.Works!)
                .Where(w => w.AssignedUserIds != null)
                .SelectMany(w => w.AssignedUserIds!)
                .Distinct()
                .ToList();

            if (allUserIds.Count == 0)
                return new Dictionary<Guid, string>();

            IEnumerable<User> users = await userRepo.GetBySearch(u => allUserIds.Contains(u.Id));
            return users.ToDictionary(u => u.Id, u => FormatFullName(u.FirstName, u.LastName));
        }

        public static (List<WorkScheduleStageWorkDependency> entities, List<WorkScheduleWorkDependencyWeb> webs) BuildDependencies(
            IEnumerable<WorkScheduleWorkDependencyDto> dependencies,
            Guid workScheduleId,
            Guid tenantId,
            Guid projectId,
            Dictionary<Guid, Guid> tempIdToWorkId)
        {
            List<WorkScheduleStageWorkDependency> entities = new List<WorkScheduleStageWorkDependency>();
            List<WorkScheduleWorkDependencyWeb> webs = new List<WorkScheduleWorkDependencyWeb>();

            foreach (WorkScheduleWorkDependencyDto dep in dependencies)
            {
                Guid? predId = dep.PredecessorDbId
                    ?? (dep.PredecessorTempId.HasValue && tempIdToWorkId.TryGetValue(dep.PredecessorTempId.Value, out Guid pId) ? pId : null);
                Guid? succId = dep.SuccessorDbId
                    ?? (dep.SuccessorTempId.HasValue && tempIdToWorkId.TryGetValue(dep.SuccessorTempId.Value, out Guid sId) ? sId : null);

                if (!predId.HasValue || !succId.HasValue)
                    continue;

                WorkScheduleStageWorkDependency entity = new WorkScheduleStageWorkDependency
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    WorkScheduleId = workScheduleId,
                    PredecessorWorkId = predId.Value,
                    SuccessorWorkId = succId.Value,
                    DependencyType = dep.DependencyType,
                    LagDays = dep.LagDays
                };
                entities.Add(entity);
                webs.Add(new WorkScheduleWorkDependencyWeb(entity.Id, predId.Value, succId.Value, dep.DependencyType, dep.LagDays));
            }

            return (entities, webs);
        }
    }
}
