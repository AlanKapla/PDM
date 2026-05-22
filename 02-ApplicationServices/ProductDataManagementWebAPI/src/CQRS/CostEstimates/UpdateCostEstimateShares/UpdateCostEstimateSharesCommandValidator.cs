using CQRS.Extensions;
using CQRS.Helpers;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateCostEstimateShares
{
    public sealed class UpdateCostEstimateSharesCommandValidator : AbstractValidator<UpdateCostEstimateSharesCommand>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<ProjectMember> projectMemberRepository;

        public UpdateCostEstimateSharesCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepository,
            IRepository<ProjectMember> projectMemberRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.projectMemberRepository = projectMemberRepository;

            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();

            RuleFor(x => x.UserIds)
                .UniqueIds()
                .When(x => x.UserIds.Count > 0);

            RuleFor(x => x.CostEstimateId)
                .MustAsync((cmd, ceId, ct) => CostEstimateShareValidationRules.CostEstimateMustExistAsync(
                    costEstimateRepository, cmd.TenantId, cmd.ProjectId, ceId, ct))
                .WithMessage("Cost estimate not found or is deleted");

            // Batch check: all provided users must be project members
            RuleFor(x => x)
                .MustAsync((cmd, ct) => CostEstimateShareValidationRules.AllUsersMustBeProjectMembersAsync(
                    projectMemberRepository, cmd.TenantId, cmd.ProjectId, cmd.UserIds, ct))
                .WithMessage("One or more users are not members of this project")
                .When(x => x.UserIds.Count > 0);
        }
    }
}
