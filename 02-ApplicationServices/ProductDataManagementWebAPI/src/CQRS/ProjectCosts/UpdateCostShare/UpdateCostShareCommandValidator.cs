using Business.Interfaces.Model;
using CQRS.Extensions;
using CQRS.ProjectCosts.Shared;
using Entities.Models.Projects;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateCostShare
{
    public sealed class UpdateCostShareCommandValidator : AbstractValidator<UpdateCostShareCommand>
    {
        public UpdateCostShareCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostId).RequiredId();

            RuleFor(x => x.SharedWithUserIds)
                .NotNull().WithMessage("SharedWithUserIds is required")
                .UniqueIds()
                .NotCurrentUser(currentUser)
                .AllAreProjectMembers(projectMemberRepo, x => x.TenantId, x => x.ProjectId);
        }
    }
}
