using FluentValidation;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.WorkSchedules.Shared
{
    #region Validation Helpers

    /// <summary>
    /// Helper class for work schedule validation logic
    /// </summary>
    public static class WorkScheduleValidationHelper
    {
        /// <summary>
        /// Checks if periods overlap with each other
        /// </summary>
        public static bool HasOverlappingPeriods<TPeriod>(
            IEnumerable<TPeriod>? periods,
            Func<TPeriod, DateTime> startDateSelector,
            Func<TPeriod, DateTime> endDateSelector)
        {
            if (periods == null)
            {
                return false;
            }

            var periodsList = periods.ToList();
            if (periodsList.Count <= 1)
            {
                return false;
            }

            var sortedPeriods = periodsList.OrderBy(startDateSelector).ToList();

            for (int i = 0; i < sortedPeriods.Count - 1; i++)
            {
                var currentPeriod = sortedPeriods[i];
                var nextPeriod = sortedPeriods[i + 1];

                if (endDateSelector(currentPeriod) > startDateSelector(nextPeriod))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates closure consistency between work and its periods
        /// </summary>
        public static bool IsClosureConsistent<TPeriod>(
            bool workIsClosed,
            IEnumerable<TPeriod>? periods,
            Func<TPeriod, bool> isClosedSelector)
        {
            if (periods == null || !periods.Any())
            {
                return true;
            }

            var periodsList = periods.ToList();
            bool allPeriodsClosed = periodsList.All(isClosedSelector);
            bool anyPeriodOpen = periodsList.Any(p => !isClosedSelector(p));

            // If all periods are closed, work must be closed
            if (allPeriodsClosed && !workIsClosed)
            {
                return false;
            }

            // If any period is open, work cannot be closed
            if (anyPeriodOpen && workIsClosed)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Flattens a tree of WorkScheduleStageDto into a single sequence (depth-first)
        /// </summary>
        public static IEnumerable<WorkScheduleStageDto> FlattenStages(IEnumerable<WorkScheduleStageDto> stages)
        {
            foreach (var stage in stages)
            {
                yield return stage;
                if (stage.Children != null)
                    foreach (var child in FlattenStages(stage.Children))
                        yield return child;
            }
        }

        /// <summary>
        /// Validates that all assigned users are project members
        /// </summary>
        public static async Task<bool> ValidateAssignedUsersAreProjectMembers<TStage, TWork>(
            IRepository<ProjectMember> projectMemberRepo,
            Guid tenantId,
            Guid projectId,
            IEnumerable<TStage>? stages,
            Func<TStage, IEnumerable<TWork>?> worksSelector,
            Func<TWork, IEnumerable<Guid>?> assignedUserIdsSelector,
            CancellationToken cancellationToken = default)
        {
            if (stages == null || !stages.Any())
            {
                return true;
            }

            // Get all unique user IDs from all works
            var allUserIds = stages
                .Where(s => worksSelector(s) != null)
                .SelectMany(s => worksSelector(s)!)
                .Where(w => assignedUserIdsSelector(w) != null)
                .SelectMany(w => assignedUserIdsSelector(w)!)
                .Distinct()
                .ToHashSet();

            if (allUserIds.Count == 0)
            {
                return true;
            }

            // Validate all assigned users are project members
            var projectMembers = (await projectMemberRepo
                .GetBySearch(
                    pm => pm.TenantId == tenantId
                        && pm.ProjectId == projectId
                        && allUserIds.Contains(pm.UserId)))
                .ToList();

            return projectMembers.Count == allUserIds.Count;
        }

        /// <summary>
        /// Builds a lookup map of all works from the stage tree, keyed by both DbId and TempId.
        /// </summary>
        public static Dictionary<Guid, WorkScheduleWorkDto> BuildWorkRefMap(IEnumerable<WorkScheduleStageDto> stages)
        {
            var map = new Dictionary<Guid, WorkScheduleWorkDto>();
            foreach (var work in FlattenStages(stages).Where(s => s.Works != null).SelectMany(s => s.Works!))
            {
                if (work.Id.HasValue) map[work.Id.Value] = work;
                if (work.TempId.HasValue) map[work.TempId.Value] = work;
            }
            return map;
        }

        /// <summary>
        /// Detects a cycle in the dependency graph built from the provided dependency DTOs.
        /// Only works visible in <paramref name="workMap"/> are included in the check.
        /// </summary>
        public static bool HasDependencyCycle(
            IEnumerable<WorkScheduleWorkDependencyDto> dependencies,
            Dictionary<Guid, WorkScheduleWorkDto> workMap)
        {
            var adjacency = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var dep in dependencies)
            {
                Guid? predKey = ResolveCanonicalKey(dep.PredecessorDbId, dep.PredecessorTempId, workMap);
                Guid? succKey = ResolveCanonicalKey(dep.SuccessorDbId, dep.SuccessorTempId, workMap);
                if (!predKey.HasValue || !succKey.HasValue) continue;

                if (!adjacency.TryGetValue(predKey.Value, out var set))
                {
                    set = new HashSet<Guid>();
                    adjacency[predKey.Value] = set;
                }
                set.Add(succKey.Value);
            }

            var visited = new HashSet<Guid>();
            var inStack = new HashSet<Guid>();

            bool HasCycle(Guid node)
            {
                if (inStack.Contains(node)) return true;
                if (visited.Contains(node)) return false;
                visited.Add(node);
                inStack.Add(node);
                if (adjacency.TryGetValue(node, out var successors))
                {
                    foreach (var succ in successors)
                        if (HasCycle(succ)) return true;
                }
                inStack.Remove(node);
                return false;
            }

            return adjacency.Keys.Any(n => !visited.Contains(n) && HasCycle(n));
        }

        /// <summary>
        /// Checks whether any dependency conflicts with the periods of the referenced works.
        /// Only validates if both works in the dependency have at least one period defined.
        /// </summary>
        public static bool HasPeriodDependencyConflict(
            IEnumerable<WorkScheduleWorkDependencyDto> dependencies,
            Dictionary<Guid, WorkScheduleWorkDto> workMap)
        {
            foreach (var dep in dependencies)
            {
                var predWork = ResolveWork(dep.PredecessorDbId, dep.PredecessorTempId, workMap);
                var succWork = ResolveWork(dep.SuccessorDbId, dep.SuccessorTempId, workMap);

                if (predWork?.Periods == null || predWork.Periods.Count == 0) continue;
                if (succWork?.Periods == null || succWork.Periods.Count == 0) continue;

                if (DependencyConflictsWithPeriods(dep, predWork.Periods, succWork.Periods))
                    return true;
            }
            return false;
        }

        private static Guid? ResolveCanonicalKey(Guid? dbId, Guid? tempId, Dictionary<Guid, WorkScheduleWorkDto> workMap)
        {
            if (dbId.HasValue && workMap.TryGetValue(dbId.Value, out var w1)) return w1.Id ?? w1.TempId;
            if (tempId.HasValue && workMap.TryGetValue(tempId.Value, out var w2)) return w2.Id ?? w2.TempId;
            return null;
        }

        private static WorkScheduleWorkDto? ResolveWork(Guid? dbId, Guid? tempId, Dictionary<Guid, WorkScheduleWorkDto> workMap)
        {
            if (dbId.HasValue && workMap.TryGetValue(dbId.Value, out var w1)) return w1;
            if (tempId.HasValue && workMap.TryGetValue(tempId.Value, out var w2)) return w2;
            return null;
        }

        private static bool DependencyConflictsWithPeriods(
            WorkScheduleWorkDependencyDto dep,
            List<WorkScheduleWorkPeriodDto> predPeriods,
            List<WorkScheduleWorkPeriodDto> succPeriods)
        {
            return dep.DependencyType switch
            {
                WorkDependencyType.FinishToStart =>
                    succPeriods.Min(p => p.StartDate) < predPeriods.Max(p => p.EndDate).AddDays(dep.LagDays),
                WorkDependencyType.StartToStart =>
                    succPeriods.Min(p => p.StartDate) < predPeriods.Min(p => p.StartDate).AddDays(dep.LagDays),
                WorkDependencyType.FinishToFinish =>
                    succPeriods.Max(p => p.EndDate) < predPeriods.Max(p => p.EndDate).AddDays(dep.LagDays),
                WorkDependencyType.StartToFinish =>
                    succPeriods.Max(p => p.EndDate) < predPeriods.Min(p => p.StartDate).AddDays(dep.LagDays),
                _ => false
            };
        }

        /// <summary>
        /// Builds the entity period list for a work item.
        /// Work-level IsClosed propagates to all periods.
        /// TenantId and ProjectId must be provided explicitly; WorkScheduleStageWorkId is set by EF via the relationship.
        /// </summary>
        public static (List<WorkScheduleStageWorkPeriod> Periods, bool IsWorkClosed) BuildPeriods(
            WorkScheduleWorkDto workDto, Guid tenantId, Guid projectId)
        {
            List<WorkScheduleStageWorkPeriod> periods = workDto.Periods?.Select(p => new WorkScheduleStageWorkPeriod
            {
                TenantId = tenantId,
                ProjectId = projectId,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsClosed = workDto.IsClosed || p.IsClosed
            }).ToList() ?? new List<WorkScheduleStageWorkPeriod>();

            bool isWorkClosed = periods.Count > 0 && periods.All(p => p.IsClosed);
            return (periods, isWorkClosed);
        }
    }

    #endregion

    #region Base Validator

    /// <summary>
    /// Base validator for WorkSchedule command containing shared validation rules for Create and Update
    /// </summary>
    /// <typeparam name="TCommand">Command type (CreateWorkScheduleCommand or UpdateWorkScheduleCommand)</typeparam>
    public abstract class WorkScheduleCommandValidatorBase<TCommand> : AbstractValidator<TCommand>
    {
        protected WorkScheduleCommandValidatorBase(IRepository<ProjectMember> projectMemberRepo)
        {
            ConfigureNameValidation();
            ConfigureProjectMembersValidation(projectMemberRepo);
            ConfigureStagesValidation();
            ConfigureDependenciesValidation();
        }

        #region Configuration Methods

        private void ConfigureNameValidation()
        {
            RuleFor(GetNameSelector())
                .NotEmpty().WithMessage("Work schedule name is required")
                .MaximumLength(200).WithMessage("Work schedule name cannot exceed 200 characters");
        }

        private void ConfigureProjectMembersValidation(IRepository<ProjectMember> projectMemberRepo)
        {
            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var stages = GetStagesSelectorFunc()(command);
                    var flatStages = stages != null
                        ? WorkScheduleValidationHelper.FlattenStages(stages)
                        : Enumerable.Empty<WorkScheduleStageDto>();
                    return await WorkScheduleValidationHelper.ValidateAssignedUsersAreProjectMembers(
                        projectMemberRepo,
                        GetTenantIdSelector()(command),
                        GetProjectIdSelector()(command),
                        flatStages,
                        stage => stage.Works,
                        work => work.AssignedUserIds,
                        cancellationToken);
                })
                .WithMessage("One or more assigned users are not members of the project");
        }

        private void ConfigureStagesValidation()
        {
            RuleForEach(GetStagesSelector())
                .SetValidator(new WorkScheduleStageDtoValidator())
                .When(x => GetStagesSelectorFunc()(x) != null);
        }

        private void ConfigureDependenciesValidation()
        {
            RuleForEach(GetDependenciesSelector())
                .SetValidator(new WorkScheduleWorkDependencyDtoValidator())
                .When(x =>
                {
                    var deps = GetDependenciesSelectorFunc()(x);
                    return deps != null && deps.Any();
                });

            RuleFor(x => x)
                .Must(cmd =>
                {
                    var deps = GetDependenciesSelectorFunc()(cmd)?.ToList();
                    if (deps == null || deps.Count == 0) return true;
                    var stages = GetStagesSelectorFunc()(cmd);
                    var workMap = stages != null
                        ? WorkScheduleValidationHelper.BuildWorkRefMap(stages)
                        : new Dictionary<Guid, WorkScheduleWorkDto>();

                    foreach (var dep in deps)
                    {
                        bool predHasTempRef = dep.PredecessorTempId.HasValue && !dep.PredecessorDbId.HasValue;
                        bool succHasTempRef = dep.SuccessorTempId.HasValue && !dep.SuccessorDbId.HasValue;

                        if (predHasTempRef && !workMap.ContainsKey(dep.PredecessorTempId!.Value))
                            return false;
                        if (succHasTempRef && !workMap.ContainsKey(dep.SuccessorTempId!.Value))
                            return false;
                    }
                    return true;
                })
                .WithMessage("One or more dependency TempId references do not match any work item in the provided stages")
                .When(x =>
                {
                    var deps = GetDependenciesSelectorFunc()(x);
                    return deps != null && deps.Any();
                });

            RuleFor(x => x)
                .Must(cmd =>
                {
                    var deps = GetDependenciesSelectorFunc()(cmd)?.ToList();
                    if (deps == null || deps.Count == 0) return true;
                    var stages = GetStagesSelectorFunc()(cmd);
                    var workMap = stages != null
                        ? WorkScheduleValidationHelper.BuildWorkRefMap(stages)
                        : new Dictionary<Guid, WorkScheduleWorkDto>();
                    return !WorkScheduleValidationHelper.HasDependencyCycle(deps, workMap);
                })
                .WithMessage("Dependencies contain a circular reference")
                .When(x =>
                {
                    var deps = GetDependenciesSelectorFunc()(x);
                    return deps != null && deps.Any();
                });

            RuleFor(x => x)
                .Must(cmd =>
                {
                    var deps = GetDependenciesSelectorFunc()(cmd)?.ToList();
                    if (deps == null || deps.Count == 0) return true;
                    var stages = GetStagesSelectorFunc()(cmd);
                    if (stages == null) return true;
                    var workMap = WorkScheduleValidationHelper.BuildWorkRefMap(stages);
                    return !WorkScheduleValidationHelper.HasPeriodDependencyConflict(deps, workMap);
                })
                .WithMessage("One or more work dependencies conflict with the defined periods")
                .When(x =>
                {
                    var deps = GetDependenciesSelectorFunc()(x);
                    return deps != null && deps.Any();
                });
        }

        #endregion

        #region Abstract Selectors

        /// <summary>
        /// Gets the expression for selecting the Name property from the command
        /// </summary>
        protected abstract Expression<Func<TCommand, string>> GetNameSelector();

        /// <summary>
        /// Gets the function for selecting TenantId from the command
        /// </summary>
        protected abstract Func<TCommand, Guid> GetTenantIdSelector();

        /// <summary>
        /// Gets the function for selecting ProjectId from the command
        /// </summary>
        protected abstract Func<TCommand, Guid> GetProjectIdSelector();

        /// <summary>
        /// Gets the expression for selecting Stages from the command (for RuleForEach)
        /// </summary>
        protected abstract Expression<Func<TCommand, IEnumerable<WorkScheduleStageDto>?>> GetStagesSelector();

        /// <summary>
        /// Gets the function for selecting Stages from the command (for ValidateAssignedUsersAreProjectMembers)
        /// </summary>
        protected abstract Func<TCommand, IEnumerable<WorkScheduleStageDto>?> GetStagesSelectorFunc();

        /// <summary>
        /// Gets the expression for selecting Dependencies from the command (for RuleForEach)
        /// </summary>
        protected abstract Expression<Func<TCommand, IEnumerable<WorkScheduleWorkDependencyDto>?>> GetDependenciesSelector();

        /// <summary>
        /// Gets the function for selecting Dependencies from the command (for cross-dependency validation)
        /// </summary>
        protected abstract Func<TCommand, IEnumerable<WorkScheduleWorkDependencyDto>?> GetDependenciesSelectorFunc();

        #endregion
    }

    #endregion

    #region DTO Validators

    /// <summary>
    /// Shared validator for WorkScheduleStageDto (used in both Create and Update)
    /// </summary>
    public class WorkScheduleStageDtoValidator : AbstractValidator<WorkScheduleStageDto>
    {
        public WorkScheduleStageDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(200).WithMessage("Stage name cannot exceed 200 characters");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Stage order must be greater than or equal to 0");

            RuleForEach(x => x.Works)
                .SetValidator(new WorkScheduleWorkDtoValidator())
                .When(x => x.Works != null);

            RuleForEach(x => x.Children)
                .SetValidator(this)
                .When(x => x.Children != null && x.Children.Count > 0);
        }
    }

    /// <summary>
    /// Shared validator for WorkScheduleWorkDto (used in both Create and Update)
    /// </summary>
    public class WorkScheduleWorkDtoValidator : AbstractValidator<WorkScheduleWorkDto>
    {
        public WorkScheduleWorkDtoValidator()
        {
            ConfigureBasicValidation();
            ConfigurePeriodsValidation();
            ConfigureClosureConsistencyValidation();
            ConfigureNestedValidation();
        }

        private void ConfigureBasicValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Work name is required")
                .MaximumLength(200).WithMessage("Work name cannot exceed 200 characters");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Work order must be greater than or equal to 0");

            RuleFor(x => x.ColorRgb)
                .NotEmpty().WithMessage("Color RGB is required")
                .MaximumLength(20).WithMessage("Color RGB cannot exceed 20 characters")
                .Matches(@"^(rgb\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*\)|#[0-9A-Fa-f]{6})$")
                .WithMessage("Color RGB must be in format 'rgb(r,g,b)' or '#RRGGBB'");
        }

        private void ConfigurePeriodsValidation()
        {
            RuleFor(x => x.Periods)
                .Must(periods => !WorkScheduleValidationHelper.HasOverlappingPeriods(
                    periods,
                    p => p.StartDate,
                    p => p.EndDate))
                .WithMessage("Periods cannot overlap with each other")
                .When(x => x.Periods != null && x.Periods.Count > 0);
        }

        private void ConfigureClosureConsistencyValidation()
        {
            RuleFor(x => x)
                .Must(work => WorkScheduleValidationHelper.IsClosureConsistent(
                    work.IsClosed,
                    work.Periods,
                    p => p.IsClosed))
                .WithMessage("Work closure status must be consistent with periods: if all periods are closed, work must be closed; if any period is open, work cannot be closed");
        }

        private void ConfigureNestedValidation()
        {
            RuleForEach(x => x.Periods)
                .SetValidator(new WorkScheduleWorkPeriodDtoValidator())
                .When(x => x.Periods != null);

            RuleForEach(x => x.Comments)
                .SetValidator(new WorkScheduleWorkCommentDtoValidator())
                .When(x => x.Comments != null);
        }
    }

    /// <summary>
    /// Shared validator for WorkScheduleWorkPeriodDto (used in both Create and Update)
    /// </summary>
    public class WorkScheduleWorkPeriodDtoValidator : AbstractValidator<WorkScheduleWorkPeriodDto>
    {
        public WorkScheduleWorkPeriodDtoValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Period start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Period end date is required")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Period end date cannot be before start date");
        }
    }

    /// <summary>
    /// Shared validator for WorkScheduleWorkCommentDto (used in both Create and Update)
    /// </summary>
    public class WorkScheduleWorkCommentDtoValidator : AbstractValidator<WorkScheduleWorkCommentDto>
    {
        public WorkScheduleWorkCommentDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required")
                .MaximumLength(2000).WithMessage("Comment content cannot exceed 2000 characters");
        }
    }

    /// <summary>
    /// Structural validator for a single WorkScheduleWorkDependencyDto.
    /// Cross-dependency rules (cycle detection, period conflicts) are validated at the command level.
    /// </summary>
    public class WorkScheduleWorkDependencyDtoValidator : AbstractValidator<WorkScheduleWorkDependencyDto>
    {
        public WorkScheduleWorkDependencyDtoValidator()
        {
            RuleFor(x => x)
                .Must(d => d.PredecessorDbId.HasValue || d.PredecessorTempId.HasValue)
                .WithMessage("A dependency must specify either PredecessorDbId or PredecessorTempId");

            RuleFor(x => x)
                .Must(d => d.SuccessorDbId.HasValue || d.SuccessorTempId.HasValue)
                .WithMessage("A dependency must specify either SuccessorDbId or SuccessorTempId");

            RuleFor(x => x)
                .Must(d =>
                {
                    if (d.PredecessorDbId.HasValue && d.PredecessorDbId == d.SuccessorDbId) return false;
                    if (d.PredecessorTempId.HasValue && d.PredecessorTempId == d.SuccessorTempId) return false;
                    return true;
                })
                .WithMessage("A work item cannot be both predecessor and successor in the same dependency");

            RuleFor(x => x.DependencyType)
                .IsInEnum()
                .WithMessage("Invalid dependency type");

            RuleFor(x => x.LagDays)
                .InclusiveBetween(-365, 365)
                .WithMessage("LagDays must be between -365 and 365");
        }
    }

    #endregion
}
