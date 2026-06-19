using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class ProjectMembershipProvisioner : IProjectMembershipProvisioner
{
    private readonly IRepository<TenantMember> tenantMemberRepo;
    private readonly IRepository<ProjectMember> projectMemberRepo;
    private readonly IRepository<ProjectMemberModulePermission> modulePermissionRepo;
    private readonly IUserService userService;
    private readonly IPermissionsVersionService permissionsVersionService;

    public ProjectMembershipProvisioner(
        IRepository<TenantMember> tenantMemberRepo,
        IRepository<ProjectMember> projectMemberRepo,
        IRepository<ProjectMemberModulePermission> modulePermissionRepo,
        IUserService userService,
        IPermissionsVersionService permissionsVersionService)
    {
        this.tenantMemberRepo = tenantMemberRepo;
        this.projectMemberRepo = projectMemberRepo;
        this.modulePermissionRepo = modulePermissionRepo;
        this.userService = userService;
        this.permissionsVersionService = permissionsVersionService;
    }

    public async Task EnsureTenantMemberAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        TenantMember? existing = await tenantMemberRepo.GetFirstBySearch(
            m => m.TenantId == tenantId && m.UserId == userId);

        if (existing is null)
        {
            TenantMember member = new TenantMember
            {
                TenantId = tenantId,
                UserId = userId,
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await tenantMemberRepo.Insert(member);
            return;
        }

        if (!existing.IsActive)
        {
            existing.IsActive = true;
            existing.IsAdmin = false;
            await tenantMemberRepo.Update(existing);
        }
    }

    public async Task ProvisionProjectMemberAsync(
        Guid tenantId,
        Guid projectId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<ProjectModule> modules,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProjectModule> effectiveModules = isAdmin
            ? modules
            : modules.Where(m => m != ProjectModule.Settings);

        ProjectMember? existing = await projectMemberRepo.GetFirstBySearch(
            pm => pm.TenantId == tenantId
                && pm.ProjectId == projectId
                && pm.UserId == userId);

        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new ConflictApiException(
                    nameof(ProjectMember),
                    userId.ToString(),
                    "User is already an active member of this project.");
            }

            existing.IsActive = true;
            existing.IsAdmin = isAdmin;
            existing.JoinedAt = DateTime.UtcNow;
            await projectMemberRepo.Update(existing);
            await ReplaceModulePermissionsAsync(tenantId, projectId, userId, effectiveModules);
        }
        else
        {
            ProjectMember newMember = new ProjectMember
            {
                TenantId = tenantId,
                ProjectId = projectId,
                UserId = userId,
                IsAdmin = isAdmin,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            await projectMemberRepo.Insert(newMember);

            foreach (ProjectModule module in effectiveModules)
            {
                await modulePermissionRepo.Insert(new ProjectMemberModulePermission
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    UserId = userId,
                    Module = module
                });
            }
        }

        await userService.InvalidateProjectMembersCacheAsync(tenantId, projectId, cancellationToken);
        await permissionsVersionService.BumpVersionAsync(userId, cancellationToken);
    }

    public async Task DeactivateAllProjectMembershipsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProjectMember> activeMemberships = await projectMemberRepo.GetBySearch(
            pm => pm.TenantId == tenantId && pm.UserId == userId && pm.IsActive);

        List<Guid> affectedProjectIds = new List<Guid>();

        foreach (ProjectMember membership in activeMemberships)
        {
            membership.IsActive = false;
            await projectMemberRepo.Update(membership);
            affectedProjectIds.Add(membership.ProjectId);
        }

        foreach (Guid projectId in affectedProjectIds.Distinct())
        {
            await userService.InvalidateProjectMembersCacheAsync(tenantId, projectId, cancellationToken);
        }

        if (affectedProjectIds.Count > 0)
        {
            await permissionsVersionService.BumpVersionAsync(userId, cancellationToken);
        }
    }

    private async Task ReplaceModulePermissionsAsync(
        Guid tenantId,
        Guid projectId,
        Guid userId,
        IEnumerable<ProjectModule> modules)
    {
        IEnumerable<ProjectMemberModulePermission> existingPermissions = await modulePermissionRepo.GetBySearch(
            mp => mp.TenantId == tenantId
                && mp.ProjectId == projectId
                && mp.UserId == userId);

        foreach (ProjectMemberModulePermission existing in existingPermissions)
        {
            await modulePermissionRepo.Delete(existing);
        }

        foreach (ProjectModule module in modules)
        {
            await modulePermissionRepo.Insert(new ProjectMemberModulePermission
            {
                TenantId = tenantId,
                ProjectId = projectId,
                UserId = userId,
                Module = module
            });
        }
    }
}
