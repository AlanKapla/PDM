using Business.Interfaces.Model;
using CQRS.Extensions;
using CQRS.ProjectCosts.Shared;
using Entities.Models.Projects;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.ShareProjectCosts
{
    public sealed class ShareProjectCostsCommandValidator : AbstractValidator<ShareProjectCostsCommand>
    {
        public ShareProjectCostsCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.ProjectCostIds)
                .NotNull().WithMessage("ProjectCostIds is required")
                .NotEmpty().WithMessage("You must select at least one cost to share")
                .Must(ids => ids.Count <= 50).WithMessage("You can share a maximum of 50 costs at once")
                .UniqueIds();

            RuleFor(x => x.SharedWithUserIds)
                .NotNull().WithMessage("SharedWithUserIds is required")
                .NotEmpty().WithMessage("You must select at least one user to share with")
                .Must(ids => ids.Count <= 50).WithMessage("You can share with a maximum of 50 users at once")
                .UniqueIds()
                .NotCurrentUser(currentUser)
                .AllAreProjectMembers(projectMemberRepo, x => x.TenantId, x => x.ProjectId);
        }
    }
}
