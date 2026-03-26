using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class UserService : IUserService
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        private readonly ICacheService cacheService;
        private readonly IRepository<ProjectMember> projectMemberRepository;
        private readonly IRepository<TenantMember> tenantMemberRepository;
        private readonly ILogger<UserService> logger;

        public UserService(
            ICacheService cacheService,
            IRepository<ProjectMember> projectMemberRepository,
            IRepository<TenantMember> tenantMemberRepository,
            ILogger<UserService> logger)
        {
            this.cacheService = cacheService;
            this.projectMemberRepository = projectMemberRepository;
            this.tenantMemberRepository = tenantMemberRepository;
            this.logger = logger;
        }

        public async Task<List<ProjectMemberUserInfo>> GetProjectMembersAsync(
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"users:{tenantId}:{projectId}:members";

            var result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug("Loading project members for project {ProjectId}", projectId);

                    var members = (await projectMemberRepository.GetBySearch(
                        pm => pm.TenantId == tenantId &&
                              pm.ProjectId == projectId &&
                              pm.TenantMember.IsActive,
                        q => q.Include(pm => pm.TenantMember)
                                  .ThenInclude(tm => tm.User),
                        q => q.Include(pm => pm.MemberRole))).ToList();

                    return members.Select(pm => new ProjectMemberUserInfo
                    {
                        UserId = pm.UserId,
                        FirstName = pm.TenantMember.User.FirstName,
                        LastName = pm.TenantMember.User.LastName,
                        Email = pm.TenantMember.User.Email,
                        AzureAdB2CObjectId = pm.TenantMember.User.AzureAdB2CObjectId,
                        RoleCode = pm.MemberRole?.Code,
                        JoinedAt = pm.JoinedAt
                    }).ToList();
                },
                CacheExpiration,
                cancellationToken);

            return result ?? [];
        }

        public async Task<ProjectMemberUserInfo?> GetProjectMemberAsync(
            Guid tenantId,
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var members = await GetProjectMembersAsync(tenantId, projectId, cancellationToken);
            return members.FirstOrDefault(m => m.UserId == userId);
        }

        public async Task InvalidateProjectMembersCacheAsync(
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"users:{tenantId}:{projectId}:members";
            await cacheService.RemoveCacheByKeyAsync(cacheKey, cancellationToken);
            logger.LogDebug("Invalidated project members cache for project {ProjectId}", projectId);
        }

        public async Task<ProjectMemberUserInfo?> GetTenantMemberInfoAsync(
            Guid tenantId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var member = await tenantMemberRepository.GetFirstBySearch(
                tm => tm.TenantId == tenantId && tm.UserId == userId,
                q => q.Include(tm => tm.User));

            if (member == null)
                return null;

            return new ProjectMemberUserInfo
            {
                UserId = member.UserId,
                FirstName = member.User.FirstName,
                LastName = member.User.LastName,
                Email = member.User.Email,
                AzureAdB2CObjectId = member.User.AzureAdB2CObjectId
            };
        }

        public async Task<Dictionary<Guid, ProjectMemberUserInfo>> GetProjectMembersByIdsAsync(
            Guid tenantId,
            Guid projectId,
            HashSet<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, ProjectMemberUserInfo>();
            }

            List<ProjectMemberUserInfo> members = await GetProjectMembersAsync(tenantId, projectId, cancellationToken);

            return members
                .Where(m => userIds.Contains(m.UserId))
                .ToDictionary(m => m.UserId);
        }
    }
}
