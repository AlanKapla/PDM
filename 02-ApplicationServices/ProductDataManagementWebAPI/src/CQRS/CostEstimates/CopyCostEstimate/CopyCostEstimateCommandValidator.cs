using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models;
using Entities.Models.CostEstimates;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CopyCostEstimate
{
    public class CopyCostEstimateCommandValidator : AbstractValidator<CopyCostEstimateCommand>
    {
        private readonly IRepository<CostEstimate> costEstimateRepo;
        private readonly IRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public CopyCostEstimateCommandValidator(
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.costEstimateRepo = costEstimateRepo;
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("CostEstimateId is required");

            RuleFor(x => x.TargetProjectIds)
                .NotEmpty().WithMessage("At least one target project is required")
                .Must(ids => ids != null && ids.Count > 0).WithMessage("At least one target project is required")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Target project IDs must be unique");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    CostEstimate? costEstimate = await costEstimateRepo.GetFirstBySearch(
                        ce => ce.Id == command.CostEstimateId
                            && ce.TenantId == command.TenantId
                            && ce.ProjectId == command.ProjectId
                            && !ce.IsDeleted
                            && ce.OwnerId == currentUser.Id);

                    return costEstimate != null;
                })
                .WithMessage("Cost estimate not found or does not belong to the tenant and project and user");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    if (command.TargetProjectIds == null || command.TargetProjectIds.Count == 0)
                        return true;

                    // Check if all target projects exist and belong to the tenant
                    var targetProjects = await projectRepo.GetBySearch(
                        p => p.TenantId == command.TenantId 
                            && command.TargetProjectIds.Contains(p.Id));

                    return targetProjects.Count() == command.TargetProjectIds.Count;
                })
                .WithMessage("One or more target projects not found");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    if (command.TargetProjectIds == null || command.TargetProjectIds.Count == 0)
                        return true;

                    // Check if user is tenant admin
                    var tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
                    bool isTenantAdmin = tenantSnapshot?.IsTenantAdmin ?? false;

                    if (isTenantAdmin)
                    {
                        // Tenant admin can copy to any project (active or inactive)
                        return true;
                    }

                    // Get all target projects with their active status
                    var targetProjects = await projectRepo.GetBySearch(
                        p => p.TenantId == command.TenantId 
                            && command.TargetProjectIds.Contains(p.Id));

                    var projectsList = targetProjects.ToList();

                    // Get user's project memberships
                    var userProjectMemberships = await projectMemberRepo.GetBySearch(
                        pm => pm.TenantId == command.TenantId
                            && command.TargetProjectIds.Contains(pm.ProjectId)
                            && pm.UserId == currentUser.Id,
                        q => q.Include(pm => pm.Project).Include(pm => pm.MemberRole));

                    var membershipsList = userProjectMemberships.ToList();

                    // For each target project, check access rights
                    foreach (var project in projectsList)
                    {
                        var membership = membershipsList.FirstOrDefault(m => m.ProjectId == project.Id);

                        if (membership == null)
                        {
                            // User is not a member of this project
                            return false;
                        }

                        if (!project.IsActive)
                        {
                            // Inactive project - only project admin can copy
                            if (membership.MemberRole?.Code.IsProjectAdmin() != true)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            // Active project - editor or admin can copy
                            if (membership.MemberRole?.Code.IsProjectAdminOrEditor() != true)
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                })
                .WithMessage("You do not have sufficient permissions to copy to one or more target projects. Editors can only copy to active projects. Inactive projects require Project Admin role");

            RuleFor(x => x)
                .Must((command) =>
                {
                    // Source project cannot be in target projects
                    return command.TargetProjectIds == null 
                        || !command.TargetProjectIds.Contains(command.ProjectId);
                })
                .WithMessage("Cannot copy cost estimate to the same project");
        }
    }
}
