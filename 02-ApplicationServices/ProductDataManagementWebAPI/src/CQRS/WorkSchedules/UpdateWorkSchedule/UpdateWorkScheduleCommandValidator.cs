using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandValidator : AbstractValidator<UpdateWorkScheduleCommand>
    {
        public UpdateWorkScheduleCommandValidator(IRepository<ProjectMember> projectMemberRepo)
        {
            RuleFor(x => x.WorkScheduleId)
                .NotEmpty().WithMessage("WorkScheduleId is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Work schedule name is required")
                .MaximumLength(200).WithMessage("Work schedule name cannot exceed 200 characters");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    if (command.Stages == null || command.Stages.Count == 0)
                        return true;

                    // Get all unique user IDs from all works
                    var allUserIds = command.Stages
                        .Where(s => s.Works != null)
                        .SelectMany(s => s.Works!)
                        .Where(w => w.AssignedUserIds != null)
                        .SelectMany(w => w.AssignedUserIds!)
                        .Distinct()
                        .ToHashSet();

                    if (allUserIds.Count == 0)
                        return true;

                    // Validate all assigned users are project members
                    var projectMembers = (await projectMemberRepo
                        .GetBySearch(
                            pm => pm.TenantId == command.TenantId
                                && pm.ProjectId == command.ProjectId
                                && allUserIds.Contains(pm.UserId)))
                        .ToList();

                    return projectMembers.Count == allUserIds.Count;
                })
                .WithMessage("One or more assigned users are not members of the project");

            RuleForEach(x => x.Stages)
                .SetValidator(new UpdateStageDtoValidator())
                .When(x => x.Stages != null);
        }
    }

    public class UpdateStageDtoValidator : AbstractValidator<UpdateStageDto>
    {
        public UpdateStageDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(200).WithMessage("Stage name cannot exceed 200 characters");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Stage order must be greater than or equal to 0");

            RuleForEach(x => x.Works)
                .SetValidator(new UpdateWorkDtoValidator())
                .When(x => x.Works != null);
        }
    }

    public class UpdateWorkDtoValidator : AbstractValidator<UpdateWorkDto>
    {
        public UpdateWorkDtoValidator()
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

            RuleFor(x => x.Periods)
                .Must(periods => !HasOverlappingPeriods(periods))
                .WithMessage("Periods cannot overlap with each other")
                .When(x => x.Periods != null && x.Periods.Count > 0);

            RuleForEach(x => x.Periods)
                .SetValidator(new UpdateWorkPeriodDtoValidator())
                .When(x => x.Periods != null);

            RuleForEach(x => x.Comments)
                .SetValidator(new UpdateWorkCommentDtoValidator())
                .When(x => x.Comments != null);
        }

        private bool HasOverlappingPeriods(List<UpdateWorkPeriodDto>? periods)
        {
            if (periods == null || periods.Count <= 1)
                return false;

            var sortedPeriods = periods.OrderBy(p => p.StartDate).ToList();

            for (int i = 0; i < sortedPeriods.Count - 1; i++)
            {
                var currentPeriod = sortedPeriods[i];
                var nextPeriod = sortedPeriods[i + 1];

                // Check if current period's end date is after or equal to next period's start date
                if (currentPeriod.EndDate > nextPeriod.StartDate)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class UpdateWorkPeriodDtoValidator : AbstractValidator<UpdateWorkPeriodDto>
    {
        public UpdateWorkPeriodDtoValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Period start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Period end date is required")
                .GreaterThan(x => x.StartDate).WithMessage("Period end date must be after start date");
        }
    }

    public class UpdateWorkCommentDtoValidator : AbstractValidator<UpdateWorkCommentDto>
    {
        public UpdateWorkCommentDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required")
                .MaximumLength(2000).WithMessage("Comment content cannot exceed 2000 characters");
        }
    }

}
