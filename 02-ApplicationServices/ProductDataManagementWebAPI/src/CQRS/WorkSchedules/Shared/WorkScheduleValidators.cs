using FluentValidation;
using Entities.Models;
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
                    return await WorkScheduleValidationHelper.ValidateAssignedUsersAreProjectMembers(
                        projectMemberRepo,
                        GetTenantIdSelector()(command),
                        GetProjectIdSelector()(command),
                        stages,
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

    #endregion
}
