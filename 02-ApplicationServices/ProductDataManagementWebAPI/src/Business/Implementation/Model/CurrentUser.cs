using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using System.Security.Claims;

namespace Business.Implementation.Model
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly IReadRepository<PermissionsVersionProfile> permissionsVersionRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Project> projectRepo;
        private readonly IUserContextCache cache;

        private bool _loaded;
        private Guid _id;
        private string? _firstName;
        private string? _lastName;
        private Guid? _activeTenantIdFromProfile;
        private SystemRole _systemRole;

        public CurrentUser(
            IHttpContextAccessor httpContextAccessor,
            IReadRepository<User> userRepo,
            IReadRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IReadRepository<PermissionsVersionProfile> permissionsVersionRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<Project> projectRepo,
            IUserContextCache cache)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userRepo = userRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.permissionsVersionRepo = permissionsVersionRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.projectRepo = projectRepo;
            this.cache = cache;
        }

        private ClaimsPrincipal HttpUser =>
            httpContextAccessor.HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity());

        public Guid Id
        {
            get
            {
                EnsureLoaded();

                if (_id == Guid.Empty)
                {
                    throw new UnauthorizedApiException();
                }

                return _id;
            }
        }

        public string AzureAdB2CObjectId =>
            HttpUser.FindFirst(ClaimNames.Oid)?.Value ?? string.Empty;

        public string FirstName
        {
            get
            {
                EnsureLoaded();
                return _firstName ?? string.Empty;
            }
        }

        public string LastName
        {
            get
            {
                EnsureLoaded();
                return _lastName ?? string.Empty;
            }
        }

        public string Email =>
            HttpUser.FindFirst(ClaimNames.PreferredUsername)?.Value ?? string.Empty;

        public Guid? ActiveTenantId
        {
            get
            {
                EnsureLoaded();
                return _activeTenantIdFromProfile;
            }
        }

        public bool IsAuthenticated =>
            HttpUser.Identity?.IsAuthenticated ?? false;

        public bool IsSuperAdmin
        {
            get
            {
                EnsureLoaded();
                return _systemRole == SystemRole.SuperAdmin;
            }
        }

        public string? GetClaimValue(string claimType)
        {
            return HttpUser.FindFirst(claimType)?.Value;
        }

        public async Task<int> GetPermissionsVersionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated) return 1;

            EnsureLoaded();

            return await cache.GetOrCreateUserPermissionsVersionAsync(
                _id,
                async () =>
                {
                    var profile = await permissionsVersionRepo.GetFirstBySearch(
                        p => p.UserId == _id,
                        cancellationToken);
                    
                    return profile?.Version ?? 1;
                },
                TimeSpan.FromSeconds(30),
                cancellationToken);
        }

        public async Task<TenantCtxSnapshot?> GetTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated)
                return null;

            EnsureLoaded();

            var version = await GetPermissionsVersionAsync(cancellationToken);

            return await cache.GetOrCreateTenantCtxAsync(
                _id,
                tenantId,
                version,
                async () => await BuildTenantSnapshotAsync(tenantId, cancellationToken),
                TimeSpan.FromMinutes(3),
                cancellationToken);
        }

        public async Task<TenantCtxSnapshot?> GetActiveTenantSnapshotAsync(CancellationToken cancellationToken = default)
        {
            if (!ActiveTenantId.HasValue)
                return null;

            return await GetTenantSnapshotAsync(ActiveTenantId.Value, cancellationToken);
        }

        public async Task<ProjectCtxSnapshot?> GetProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated || !ActiveTenantId.HasValue)
                return null;

            EnsureLoaded();

            var version = await GetPermissionsVersionAsync(cancellationToken);

            return await cache.GetOrCreateProjectCtxAsync(
                _id,
                ActiveTenantId.Value,
                projectId,
                version,
                async () => await BuildProjectSnapshotAsync(projectId, cancellationToken),
                TimeSpan.FromMinutes(3),
                cancellationToken);
        }

        public async Task<ProjectCtxSnapshot?> GetProjectSnapshotWithoutActiveTenantAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated)
                return null;

            EnsureLoaded();

            var version = await GetPermissionsVersionAsync(cancellationToken);

            // Use Guid.Empty as the tenantId sentinel — cache key does not overlap with GetProjectSnapshotAsync
            return await cache.GetOrCreateProjectCtxAsync(
                _id,
                Guid.Empty,
                projectId,
                version,
                async () => await BuildProjectSnapshotAsync(projectId, cancellationToken),
                TimeSpan.FromMinutes(3),
                cancellationToken);
        }

        private async Task<TenantCtxSnapshot> BuildTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            TenantMember? membership = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == tenantId && tm.UserId == _id && tm.IsActive);

            if (membership is null)
            {
                if (_systemRole == SystemRole.SuperAdmin)
                {
                    return new TenantCtxSnapshot(tenantId, IsAdmin: false, IsActive: true);
                }

                throw new InvalidOperationException("User is not a member of the tenant");
            }

            bool isAdmin = membership.IsAdmin || _systemRole == SystemRole.SuperAdmin;

            return new TenantCtxSnapshot(tenantId, IsAdmin: isAdmin, IsActive: true);
        }

        private async Task<ProjectCtxSnapshot> BuildProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken)
        {
            // Get project to obtain tenantId and IsActive
            var projectEntity = await projectRepo.GetFirstBySearch(
                p => p.Id == projectId,
                cancellationToken);

            if (projectEntity == null)
            {
                throw new InvalidOperationException($"Project {projectId} not found");
            }

            // Check if user is Tenant Admin (has admin rights in project's tenant)
            TenantMember? tenantMembership = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == projectEntity.TenantId && tm.UserId == _id);

            bool isTenantAdmin = tenantMembership?.IsAdmin ?? false;

            // Check project membership with ModulePermissions
            var membership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == projectId && pm.UserId == _id,
                q => q.Include(pm => pm.ModulePermissions));

            var permissions = new HashSet<string>();
            bool isProjectAdmin = false;

            // ────────────────────────────────────────────────────────────────────────
            // STEP 1: SuperAdmin - Always add fallback permissions if SuperAdmin
            // ────────────────────────────────────────────────────────────────────────
            if (_systemRole == SystemRole.SuperAdmin)
            {
                foreach (string fallbackPermission in SuperAdminFallbackPermissions.ProjectReadOnly)
                {
                    permissions.Add(fallbackPermission);
                }
            }

            // ────────────────────────────────────────────────────────────────────────
            // STEP 2: Tenant Admin - Add all admin-level module permissions
            // ────────────────────────────────────────────────────────────────────────
            if (isTenantAdmin)
            {
                foreach (string adminPermission in ModulePermissionTranslator.GetAllModulePermissions())
                {
                    permissions.Add(adminPermission);
                }

                permissions.Add(PermissionCodes.ProjectMembers); // admin-only, not a configurable module
                isProjectAdmin = true;
            }

            // ────────────────────────────────────────────────────────────────────────
            // STEP 3: Project Membership - Translate ModulePermissions to codes
            // ────────────────────────────────────────────────────────────────────────
            if (membership is not null)
            {
                // Every project member can fetch basic project details regardless of module config
                permissions.Add(PermissionCodes.ProjectView);

                if (membership.IsAdmin)
                {
                    // IsAdmin flag grants full access to all modules (overrides ModulePermissions)
                    foreach (string adminPermission in ModulePermissionTranslator.GetAllModulePermissions())
                    {
                        permissions.Add(adminPermission);
                    }

                    permissions.Add(PermissionCodes.ProjectMembers); // admin-only, not a configurable module
                }
                else
                {
                    foreach (var mp in membership.ModulePermissions)
                    {
                        foreach (string code in ModulePermissionTranslator.Translate(mp.Module))
                        {
                            permissions.Add(code);
                        }
                    }
                }

                isProjectAdmin = isProjectAdmin || membership.IsAdmin;
            }

            // ────────────────────────────────────────────────────────────────────────
            // VALIDATION: User must have some source of permissions
            // (catches users who are not member, not tenant admin, and not superadmin)
            // ────────────────────────────────────────────────────────────────────────
            if (permissions.Count == 0)
            {
                throw new InvalidOperationException("User is not a member of the project");
            }

            return new ProjectCtxSnapshot(
                projectId,
                projectEntity.TenantId,
                permissions,
                isProjectAdmin,
                projectEntity.IsActive
            );
        }

        private void EnsureLoaded()
        {
            if (_loaded)
                return;

            string? azureB2CObjectId = HttpUser.FindFirst(ClaimNames.Oid)?.Value;

            if (string.IsNullOrEmpty(azureB2CObjectId))
            {
                _loaded = true;
                return;
            }

            User? user = userRepo.GetFirstBySearch(u => u.AzureAdB2CObjectId == azureB2CObjectId)
                .GetAwaiter().GetResult();

            if (user != null)
            {
                _id = user.Id;
                _firstName = user.FirstName;
                _lastName = user.LastName;
                _systemRole = user.SystemRole;

                TenantPreferencesProfile? prefs = tenantPrefsRepo.GetFirstBySearch(p => p.UserId == user.Id)
                    .GetAwaiter().GetResult();
                
                if (prefs != null)
                {
                    _activeTenantIdFromProfile = prefs.ActiveTenantId;
                }
            }

            _loaded = true;
        }

        public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            TenantCtxSnapshot? snapshot = await GetTenantSnapshotAsync(tenantId, cancellationToken);
            return snapshot?.IsAdmin ?? false;
        }

        public async Task<bool> IsProjectAdminAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var projectSnapshot = await GetProjectSnapshotAsync(projectId, cancellationToken);
            return projectSnapshot?.IsProjectAdmin ?? false;
        }

        public async Task<bool> IsTenantOrProjectAdminAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
        {
            var isTenantAdmin = await IsTenantAdminAsync(tenantId, cancellationToken);
            if (isTenantAdmin)
            {
                return true;
            }

            var isProjectAdmin = await IsProjectAdminAsync(projectId, cancellationToken);
            return isProjectAdmin;
        }
    }
}
