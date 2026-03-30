using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class ProjectMemberService : IProjectMemberService
{
    private readonly IRepository<ProjectMember> projectMemberRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly IReadRepository<Project> projectRepo;
    private readonly IReadRepository<Tenant> tenantRepo;

    public ProjectMemberService(
        IRepository<ProjectMember> projectMemberRepo,
        IReadRepository<User> userRepo,
        IReadRepository<Project> projectRepo,
        IReadRepository<Tenant> tenantRepo)
    {
        this.projectMemberRepo = projectMemberRepo;
        this.userRepo = userRepo;
        this.projectRepo = projectRepo;
        this.tenantRepo = tenantRepo;
    }

    public async Task<ProjectMember?> FindSharedProjectAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken cancellationToken = default)
    {
        List<Guid> user1ProjectIds = await projectMemberRepo.SelectAsync(
            pm => pm.UserId == userId1,
            pm => pm.ProjectId,
            cancellationToken);

        if (user1ProjectIds.Count == 0)
        {
            return null;
        }

        return await projectMemberRepo.GetFirstBySearch(
            pm => pm.UserId == userId2 && user1ProjectIds.Contains(pm.ProjectId));
    }

    public async Task<ProjectMember?> FindCommonProjectForAllAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> allUserIds = userIds.Distinct().ToList();

        if (allUserIds.Count == 0)
        {
            return null;
        }

        List<Guid> firstUserProjectIds = await projectMemberRepo.SelectAsync(
            pm => pm.UserId == allUserIds[0],
            pm => pm.ProjectId,
            cancellationToken);

        foreach (Guid projectId in firstUserProjectIds)
        {
            int count = await projectMemberRepo.CountAsync(
                pm => pm.ProjectId == projectId && allUserIds.Contains(pm.UserId),
                cancellationToken);

            if (count == allUserIds.Count)
            {
                return await projectMemberRepo.GetFirstBySearch(
                    pm => pm.ProjectId == projectId && pm.UserId == allUserIds[0]);
            }
        }

        return null;
    }

    public Task<bool> IsUserInProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return projectMemberRepo.AnyAsync(
            pm => pm.ProjectId == projectId && pm.UserId == userId,
            cancellationToken);
    }

    public async Task<string> GetUserDisplayNameAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User? user = await userRepo.GetById(userId);
        return user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId.ToString();
    }

    public async Task<Dictionary<Guid, (string FirstName, string LastName)>> GetUserNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        IEnumerable<User> users = await userRepo.GetBySearch(u => ids.Contains(u.Id));
        return users.ToDictionary(u => u.Id, u => (u.FirstName, u.LastName));
    }

    public async Task<List<ProjectMembersGroupDto>> GetUserProjectGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> myProjectIds = await projectMemberRepo.SelectAsync(
            pm => pm.UserId == userId,
            pm => pm.ProjectId,
            cancellationToken);

        if (myProjectIds.Count == 0)
        {
            return new();
        }

        IEnumerable<Project> projects = await projectRepo.GetBySearch(p => myProjectIds.Contains(p.Id));
        List<Guid> tenantIds = projects.Select(p => p.TenantId).Distinct().ToList();

        IEnumerable<Tenant> tenants = await tenantRepo.GetBySearch(t => tenantIds.Contains(t.Id));
        Dictionary<Guid, string> tenantNames = tenants.ToDictionary(t => t.Id, t => t.Name);

        IEnumerable<ProjectMember> allMembers = await projectMemberRepo.GetBySearch(
            pm => myProjectIds.Contains(pm.ProjectId) && pm.UserId != userId);

        Dictionary<Guid, List<Guid>> membersByProject = allMembers
            .GroupBy(pm => pm.ProjectId)
            .ToDictionary(g => g.Key, g => g.Select(pm => pm.UserId).Distinct().ToList());

        return projects
            .Select(p => new ProjectMembersGroupDto(
                p.Id,
                p.Name,
                p.TenantId,
                tenantNames.GetValueOrDefault(p.TenantId) ?? string.Empty,
                membersByProject.GetValueOrDefault(p.Id) ?? new()))
            .ToList();
    }

    public async Task<bool> AreAllMembersOfProjectAsync(
        Guid projectId,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return true;
        }

        int count = await projectMemberRepo.CountAsync(
            pm => pm.ProjectId == projectId && ids.Contains(pm.UserId),
            cancellationToken);

        return count == ids.Count;
    }

    public async Task<List<(Guid UserId, string FirstName, string LastName)>> GetProjectMembersExcludingAsync(
        Guid projectId,
        IEnumerable<Guid> excludeUserIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> excludeIds = excludeUserIds.Distinct().ToList();

        List<Guid> projectUserIds = await projectMemberRepo.SelectAsync(
            pm => pm.ProjectId == projectId && !excludeIds.Contains(pm.UserId),
            pm => pm.UserId,
            cancellationToken);

        if (projectUserIds.Count == 0)
        {
            return new();
        }

        IEnumerable<User> users = await userRepo.GetBySearch(u => projectUserIds.Contains(u.Id));
        return users.Select(u => (u.Id, u.FirstName, u.LastName)).ToList();
    }

    public async Task<Guid?> GetProjectTenantIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        Project? project = await projectRepo.GetById(projectId);
        return project?.TenantId;
    }
}
