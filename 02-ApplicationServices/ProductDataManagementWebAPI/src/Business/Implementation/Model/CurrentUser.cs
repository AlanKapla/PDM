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

        public async Task<TenantCtxSnapshot?> GetActiveTenantSnapshotAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated || !ActiveTenantId.HasValue)
                return null;

            EnsureLoaded();

            var version = await GetPermissionsVersionAsync(cancellationToken);

            return await cache.GetOrCreateTenantCtxAsync(
                _id,
                ActiveTenantId.Value,
                version,
                async () => await BuildTenantSnapshotAsync(ActiveTenantId.Value, cancellationToken),
                TimeSpan.FromMinutes(3),
                cancellationToken);
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
            var membership = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == tenantId && tm.UserId == _id,
                q => q.Include(tm => tm.MemberRole!));

            if (membership?.RoleId == null)
            {
                // If not a member but is SuperAdmin, grant read-only permissions
                if (_systemRole == SystemRole.SuperAdmin)
                {
                    return new TenantCtxSnapshot(
                        tenantId,
                        Guid.Empty, // No role ID for non-member SuperAdmin
                        SuperAdminFallbackPermissions.TenantReadOnly,
                        false // Not a tenant admin (no membership)
                    );
                }

                throw new InvalidOperationException("User is not a member of the tenant");
            }

            var permissions = await GetRolePermissionsAsync(membership.RoleId.Value, cancellationToken);

            var isTenantAdmin = membership.MemberRole?.Code == RoleCodes.TenantAdmin || membership.MemberRole?.Code == RoleCodes.SystemSuperAdmin;

            return new TenantCtxSnapshot(
                tenantId,
                membership.RoleId.Value,
                permissions,
                isTenantAdmin);
        }

        private async Task<ProjectCtxSnapshot> BuildProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken)
        {
            var membership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == projectId && pm.UserId == _id,
                q => q.Include(pm => pm.MemberRole!));

            if (membership == null)
            {
                // If not a member but is SuperAdmin, grant read-only permissions
                if (_systemRole == SystemRole.SuperAdmin)
                {
                    // Get project to obtain tenantId
                    var project = await projectRepo.GetFirstBySearch(
                        p => p.Id == projectId,
                        cancellationToken);
                    
                    if (project == null)
                    {
                        throw new InvalidOperationException($"Project {projectId} not found");
                    }

                    return new ProjectCtxSnapshot(
                        projectId,
                        project.TenantId,
                        null, // No role ID for non-member SuperAdmin
                        SuperAdminFallbackPermissions.ProjectReadOnly,
                        false // Not a project admin (no membership)
                    );
                }

                throw new InvalidOperationException("User is not a member of the project");
            }

            // Get project to obtain tenantId
            var projectEntity = await projectRepo.GetFirstBySearch(
                p => p.Id == projectId,
                cancellationToken);

            if (projectEntity == null)
            {
                throw new InvalidOperationException($"Project {projectId} not found");
            }

            var permissions = membership.RoleId.HasValue
                ? await GetRolePermissionsAsync(membership.RoleId.Value, cancellationToken)
                : new HashSet<string>();

            var isProjectAdmin = membership.MemberRole?.Code == RoleCodes.ProjectAdmin || membership.MemberRole?.Code == RoleCodes.SystemSuperAdmin;

            return new ProjectCtxSnapshot(
                projectId,
                projectEntity.TenantId,
                membership.RoleId,
                permissions,
                isProjectAdmin);
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
    }
}
