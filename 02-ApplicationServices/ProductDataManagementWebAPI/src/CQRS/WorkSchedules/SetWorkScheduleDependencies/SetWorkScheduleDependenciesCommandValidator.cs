using CQRS.Extensions;
using CQRS.WorkSchedules.Shared;
using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleDependencies
{
    public sealed class SetWorkScheduleDependenciesCommandValidator : AbstractValidator<SetWorkScheduleDependenciesCommand>
    {
        public SetWorkScheduleDependenciesCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.Dependencies).NotNull();

            RuleForEach(x => x.Dependencies).ChildRules(dep =>
            {
                dep.RuleFor(d => d.PredecessorWorkId).RequiredId();
                dep.RuleFor(d => d.SuccessorWorkId).RequiredId();
                dep.RuleFor(d => d)
                    .Must(d => d.PredecessorWorkId != d.SuccessorWorkId)
                    .WithName("Dependency")
                    .WithMessage("PredecessorWorkId and SuccessorWorkId must not be the same.");
            });

            RuleFor(x => x.Dependencies)
                .Must(HaveNoDuplicatePairs)
                .WithMessage("Dependencies must not contain duplicate (PredecessorWorkId, SuccessorWorkId) pairs.");
        }

        private static bool HaveNoDuplicatePairs(List<WorkDependencyDto> dependencies)
        {
            if (dependencies == null || dependencies.Count < 2)
            {
                return true;
            }

            HashSet<(Guid, Guid)> seen = new HashSet<(Guid, Guid)>();

            foreach (WorkDependencyDto dto in dependencies)
            {
                if (!seen.Add((dto.PredecessorWorkId, dto.SuccessorWorkId)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
