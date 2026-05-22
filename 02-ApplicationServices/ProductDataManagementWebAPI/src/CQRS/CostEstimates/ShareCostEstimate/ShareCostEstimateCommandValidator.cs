using CQRS.Extensions;
using CQRS.Helpers;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ShareCostEstimate
{
    public sealed class ShareCostEstimateCommandValidator : AbstractValidator<ShareCostEstimateCommand>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<ProjectMember> projectMemberRepository;

        public ShareCostEstimateCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepository,
            IRepository<ProjectMember> projectMemberRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.projectMemberRepository = projectMemberRepository;

            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();

            RuleFor(x => x.ShareWithUserIds)
                .NotEmpty().WithMessage("At least one user must be specified");

            RuleFor(x => x.ShareWithUserIds)
                .UniqueIds()
                .When(x => x.ShareWithUserIds.Count > 0);

            RuleFor(x => x.CostEstimateId)
                .MustAsync((cmd, ceId, ct) => CostEstimateShareValidationRules.CostEstimateMustExistAsync(
                    costEstimateRepository, cmd.TenantId, cmd.ProjectId, ceId, ct))
                .WithMessage("Cost estimate not found or is deleted");

            // Single batch query instead of N+1 per user
            RuleFor(x => x)
                .MustAsync((cmd, ct) => CostEstimateShareValidationRules.AllUsersMustBeProjectMembersAsync(
                    projectMemberRepository, cmd.TenantId, cmd.ProjectId, cmd.ShareWithUserIds, ct))
                .WithMessage("One or more users are not members of this project")
                .When(x => x.ShareWithUserIds.Count > 0);
        }
    }
}
