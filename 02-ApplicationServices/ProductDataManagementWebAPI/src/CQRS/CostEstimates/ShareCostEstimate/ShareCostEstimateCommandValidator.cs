using CQRS.Helpers;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ShareCostEstimate
{
    public class ShareCostEstimateCommandValidator : AbstractValidator<ShareCostEstimateCommand>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<ProjectMember> projectMemberRepository;

        public ShareCostEstimateCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepository,
            IRepository<ProjectMember> projectMemberRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.projectMemberRepository = projectMemberRepository;

            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required");
            RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId is required");
            RuleFor(x => x.CostEstimateId).NotEmpty().WithMessage("CostEstimateId is required");

            RuleFor(x => x.ShareWithUserIds)
                .NotEmpty().WithMessage("At least one user must be specified");

            RuleFor(x => x.ShareWithUserIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("User IDs must be unique")
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
