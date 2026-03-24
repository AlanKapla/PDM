using CQRS.Helpers;
using Entities.Models;
using Entities.Models.CostEstimates;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateCostEstimateShares
{
    public class UpdateCostEstimateSharesCommandValidator : AbstractValidator<UpdateCostEstimateSharesCommand>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<ProjectMember> projectMemberRepository;

        public UpdateCostEstimateSharesCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepository,
            IRepository<ProjectMember> projectMemberRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.projectMemberRepository = projectMemberRepository;

            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required");
            RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId is required");
            RuleFor(x => x.CostEstimateId).NotEmpty().WithMessage("CostEstimateId is required");

            RuleFor(x => x.UserIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("User IDs must be unique")
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
