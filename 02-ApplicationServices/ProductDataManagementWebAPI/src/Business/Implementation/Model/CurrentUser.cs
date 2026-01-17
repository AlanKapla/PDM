using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;
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
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly IRepository<RolePermission> rolePermissionRepo;
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
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<Role> roleRepo,
            IRepository<RolePermission> rolePermissionRepo,
            IUserContextCache cache)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userRepo = userRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.permissionsVersionRepo = permissionsVersionRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.projectRepo = projectRepo;
            this.tenantRepo = tenantRepo;
            this.roleRepo = roleRepo;
            this.rolePermissionRepo = rolePermissionRepo;
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

        private async Task<TenantCtxSnapshot> BuildTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            // Load tenant to get IsActive
            var tenantEntity = await tenantRepo.GetFirstBySearch(
                t => t.Id == tenantId,
                cancellationToken);

            if (tenantEntity == null)
            {
                throw new InvalidOperationException($"Tenant {tenantId} not found");
            }

            var membership = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == tenantId && tm.UserId == _id,
                q => q.Include(tm => tm.MemberRole!));

            // No membership - check if SuperAdmin for fallback access
            if (membership?.RoleId == null)
            {
                if (_systemRole == SystemRole.SuperAdmin)
                {
                    return new TenantCtxSnapshot(
                        tenantId,
                        Guid.Empty, // No role ID for non-member SuperAdmin
                        SuperAdminFallbackPermissions.TenantReadOnly,
                        false, // Not a tenant admin (no membership)
                        tenantEntity.IsActive // Include IsActive from tenant
                    );
                }

                throw new InvalidOperationException("User is not a member of the tenant");
            }

            // Step 1: Start with permissions from tenant role
            var permissions = await GetRolePermissionsAsync(membership.RoleId.Value, cancellationToken);

            // Step 2: If SuperAdmin, ALWAYS add fallback permissions (independent of other roles)
            if (_systemRole == SystemRole.SuperAdmin)
            {
                foreach (var fallbackPermission in SuperAdminFallbackPermissions.TenantReadOnly)
                {
                    permissions.Add(fallbackPermission);
                }
            }

            var isTenantAdmin = membership.MemberRole?.Code == RoleCodes.TenantAdmin;

            return new TenantCtxSnapshot(
                tenantId,
                membership.RoleId.Value,
                permissions,
                isTenantAdmin,
                tenantEntity.IsActive // Include IsActive from tenant
            );
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

            // Check project membership
            var membership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == projectId && pm.UserId == _id,
                q => q.Include(pm => pm.MemberRole!));

            // Check if user is Tenant Admin (has admin rights in project's tenant)
            var tenantMembership = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == projectEntity.TenantId && tm.UserId == _id,
                q => q.Include(tm => tm.MemberRole!));

            bool isTenantAdmin = tenantMembership?.MemberRole?.Code == RoleCodes.TenantAdmin;

            // ========================================================================
            // DECISION TREE: Build permissions based on user's roles
            // Order: SuperAdmin fallback → Tenant Admin → Project Role
            // All are additive (Union), not mutually exclusive
            // ========================================================================

            var permissions = new HashSet<string>();
            Guid? projectRoleId = membership?.RoleId;
            bool isProjectAdmin = false;

            // ────────────────────────────────────────────────────────────────────────
            // STEP 1: SuperAdmin - Always add fallback permissions if SuperAdmin
            // ────────────────────────────────────────────────────────────────────────
            if (_systemRole == SystemRole.SuperAdmin)
            {
                // SuperAdmin gets read-only fallback permissions
                // These are ALWAYS added, regardless of Tenant Admin or Project membership
                foreach (var fallbackPermission in SuperAdminFallbackPermissions.ProjectReadOnly)
                {
                    permissions.Add(fallbackPermission);
                }
            }

            // ────────────────────────────────────────────────────────────────────────
            // STEP 2: Tenant Admin - Add full PROJECT.ADMIN permissions for tenant's projects
            // ────────────────────────────────────────────────────────────────────────
            if (isTenantAdmin)
            {
                // Tenant Admin gets full PROJECT.ADMIN permissions in their tenant's projects
                var adminRole = await roleRepo.GetFirstBySearch(
                    r => r.Scope == RoleScope.Project && r.Code == RoleCodes.ProjectAdmin,
                    cancellationToken);

                if (adminRole == null)
                {
                    throw new InvalidOperationException($"{RoleCodes.ProjectAdmin} role not found");
                }

                var adminPermissions = await GetRolePermissionsAsync(adminRole.Id, cancellationToken);
                
                // Merge admin permissions
                foreach (var adminPermission in adminPermissions)
                {
                    permissions.Add(adminPermission);
                }

                // Tenant Admin is treated as Project Admin
                isProjectAdmin = true;
            }

            // ────────────────────────────────────────────────────────────────────────
            // STEP 3: Project Membership - Add permissions from project role
            // ────────────────────────────────────────────────────────────────────────
            if (membership?.RoleId.HasValue == true)
            {
                // User has project membership - add role permissions
                var rolePermissions = await GetRolePermissionsAsync(membership.RoleId.Value, cancellationToken);
                
                foreach (var rolePermission in rolePermissions)
                {
                    permissions.Add(rolePermission);
                }

                // Check if user is Project Admin through project role
                isProjectAdmin = membership.MemberRole?.Code == RoleCodes.ProjectAdmin;
            }

            // ────────────────────────────────────────────────────────────────────────
            // VALIDATION: User must have at least one source of permissions
            // ────────────────────────────────────────────────────────────────────────
            if (permissions.Count == 0)
            {
                // No permissions from any source - user has no access
                throw new InvalidOperationException("User is not a member of the project");
            }

            // ────────────────────────────────────────────────────────────────────────
            // RETURN: ProjectCtxSnapshot with merged permissions from all sources
            // ────────────────────────────────────────────────────────────────────────
            return new ProjectCtxSnapshot(
                projectId,
                projectEntity.TenantId,
                projectRoleId, // Null if no project membership
                permissions,   // Union of all permissions
                isProjectAdmin, // True if Tenant Admin OR Project Admin role
                projectEntity.IsActive // Include IsActive from project
            );
        }

        private async Task<HashSet<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await cache.GetOrCreateRolePermissionsAsync(
                roleId,
                async () =>
                {
                    var rolePermissions = await rolePermissionRepo.GetBySearch(
                        rp => rp.RoleId == roleId,
                        q => q.Include(rp => rp.Permission));

                    return rolePermissions
                        .Where(rp => rp.Permission.IsActive)
                        .Select(rp => rp.Permission.Code)
                        .ToHashSet();
                },
                TimeSpan.FromMinutes(10),
                cancellationToken);
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
            var tenantSnapshot = await GetTenantSnapshotAsync(tenantId, cancellationToken);
            return tenantSnapshot?.IsTenantAdmin ?? false;
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
