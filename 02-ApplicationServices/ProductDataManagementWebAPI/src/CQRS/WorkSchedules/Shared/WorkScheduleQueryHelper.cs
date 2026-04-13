using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using Entities.Models;

namespace CQRS.WorkSchedules.Shared
{
    public static class WorkScheduleQueryHelper
    {
        public static string FormatFullName(string? firstName, string? lastName) =>
            $"{firstName} {lastName}".Trim();

        public static async Task<Dictionary<Guid, string>> BuildUserNameDictAsync(
            IUserService userService,
            Guid tenantId,
            Guid projectId,
            IEnumerable<WorkScheduleStageDto> stages,
            CancellationToken cancellationToken)
        {
            HashSet<Guid> allUserIds = WorkScheduleValidationHelper.FlattenStages(stages)
                .Where(s => s.Works != null)
                .SelectMany(s => s.Works!)
                .Where(w => w.AssignedUserIds != null)
                .SelectMany(w => w.AssignedUserIds!)
                .ToHashSet();

            if (allUserIds.Count == 0)
                return new Dictionary<Guid, string>();

            Dictionary<Guid, ProjectMemberUserInfo> members = await userService.GetProjectMembersByIdsAsync(
                tenantId, projectId, allUserIds, cancellationToken);

            return members.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FullName);
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

                if (!predId.HasValue)
                    throw new ValidationApiException(
                        $"Nie można rozwiązać poprzednika zależności: DbId={dep.PredecessorDbId}, TempId={dep.PredecessorTempId}");

                if (!succId.HasValue)
                    throw new ValidationApiException(
                        $"Nie można rozwiązać następnika zależności: DbId={dep.SuccessorDbId}, TempId={dep.SuccessorTempId}");

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
