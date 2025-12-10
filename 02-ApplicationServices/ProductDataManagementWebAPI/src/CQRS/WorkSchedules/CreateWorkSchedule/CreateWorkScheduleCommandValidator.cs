using FluentValidation;
using Entities.Models;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandValidator : AbstractValidator<CreateWorkScheduleCommand>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;

        public CreateWorkScheduleCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Work schedule name is required")
                .MaximumLength(200).WithMessage("Work schedule name cannot exceed 200 characters");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    Project? project = await projectRepo.GetFirstBySearch(
                        p => p.Id == command.ProjectId && p.TenantId == command.TenantId && p.IsActive,
                        cancellationToken);

                    return project != null;
                })
                .WithMessage("Project not found or does not belong to the tenant");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    if (command.Stages == null)
                        return true;

                    // Get all unique user IDs from all works
                    var allUserIds = command.Stages
                        .SelectMany(s => s.Works)
                        .SelectMany(w => w.AssignedUserIds)
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

            RuleFor(x => x.Stages)
                .NotEmpty().WithMessage("At least one stage is required");

            RuleForEach(x => x.Stages)
                .SetValidator(new CreateStageDtoValidator());
        }
    }

    public class CreateStageDtoValidator : AbstractValidator<CreateStageDto>
    {
        public CreateStageDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(200).WithMessage("Stage name cannot exceed 200 characters");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Stage order must be greater than or equal to 0");

            RuleFor(x => x.Works)
                .NotEmpty().WithMessage("At least one work is required in each stage");

            RuleForEach(x => x.Works)
                .SetValidator(new CreateWorkDtoValidator());
        }
    }

    public class CreateWorkDtoValidator : AbstractValidator<CreateWorkDto>
    {
        public CreateWorkDtoValidator()
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
                .NotEmpty().WithMessage("At least one period is required for each work")
                .Must(periods => !HasOverlappingPeriods(periods))
                .WithMessage("Periods cannot overlap with each other");

            RuleForEach(x => x.Periods)
                .SetValidator(new CreateWorkPeriodDtoValidator());

            RuleFor(x => x.AssignedUserIds)
                .NotEmpty().WithMessage("At least one user must be assigned to each work");
        }

        private bool HasOverlappingPeriods(List<CreateWorkPeriodDto> periods)
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

    public class CreateWorkPeriodDtoValidator : AbstractValidator<CreateWorkPeriodDto>
    {
        public CreateWorkPeriodDtoValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Period start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Period end date is required")
                .GreaterThan(x => x.StartDate).WithMessage("Period end date must be after start date");
        }
    }
}
