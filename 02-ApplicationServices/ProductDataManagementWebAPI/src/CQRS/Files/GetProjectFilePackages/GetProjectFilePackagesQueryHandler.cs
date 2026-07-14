using Business.Implementation.Services.Files;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using MediatR;

namespace CQRS.Files.GetProjectFilePackages;

public sealed class GetProjectFilePackagesQueryHandler : IRequestHandler<GetProjectFilePackagesQuery, List<ProjectFilePackageWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IUserService userService;
    private readonly ICurrentUser currentUser;

    public GetProjectFilePackagesQueryHandler(
        IProjectFilesService projectFilesService,
        IUserService userService,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.userService = userService;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFilePackageWeb>> Handle(GetProjectFilePackagesQuery request, CancellationToken cancellationToken)
    {
        Dictionary<Guid, ProjectFilePackageDto> accessiblePackages = await projectFilesService.GetAccessiblePackagesAsync(
            currentUser,
            request.TenantId,
            request.ProjectId,
            request.Scope,
            cancellationToken);

        if (accessiblePackages.Count == 0)
        {
            return new List<ProjectFilePackageWeb>();
        }

        Dictionary<Guid, int> fileCountDict = await projectFilesService.GetAccessibleFileCountsAsync(
            currentUser,
            request.TenantId,
            request.ProjectId,
            accessiblePackages.Keys.ToHashSet(),
            request.Scope,
            cancellationToken);

        HashSet<Guid> ownerIds = accessiblePackages.Values
            .Select(p => p.OwnerId)
            .ToHashSet();

        Dictionary<Guid, ProjectMemberUserInfo> userDict = await userService.GetProjectMembersByIdsAsync(
            request.TenantId,
            request.ProjectId,
            ownerIds,
            cancellationToken);

        Dictionary<Guid, ProjectFilePackageWeb> webNodesById = accessiblePackages.ToDictionary(
            kvp => kvp.Key,
            kvp => MapToPackageWeb(kvp.Value, userDict, fileCountDict.GetValueOrDefault(kvp.Key, 0)));

        HashSet<Guid> attachedAsChild = new HashSet<Guid>();

        foreach (ProjectFilePackageWeb node in webNodesById.Values)
        {
            if (node.ParentId.HasValue && webNodesById.TryGetValue(node.ParentId.Value, out ProjectFilePackageWeb? parent))
            {
                parent.SubCatalogs.Add(node);
                attachedAsChild.Add(node.Id);
            }
        }

        List<ProjectFilePackageWeb> rootNodes = webNodesById.Values
            .Where(n => !attachedAsChild.Contains(n.Id))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        // Propagate file counts upwards so each node shows total for itself + all descendants
        foreach (ProjectFilePackageWeb root in rootNodes)
        {
            AddDescendantFileCounts(root);
        }

        return rootNodes;
    }

    /// <summary>
    /// Recursively sums TotalFiles from all descendants into each ancestor node.
    /// Returns the total (own + all descendants) for the given node.
    /// </summary>
    private static int AddDescendantFileCounts(ProjectFilePackageWeb node)
    {
        int sum = node.TotalFiles;
        foreach (ProjectFilePackageWeb child in node.SubCatalogs)
        {
            sum += AddDescendantFileCounts(child);
        }
        node.TotalFiles = sum;
        return sum;
    }

    private static ProjectFilePackageWeb MapToPackageWeb(
        ProjectFilePackageDto package,
        IReadOnlyDictionary<Guid, ProjectMemberUserInfo> userDict,
        int totalFiles)
    {
        return new ProjectFilePackageWeb
        {
            Id = package.Id,
            Name = package.Name,
            CreatedAt = package.CreatedAt,
            OwnerId = package.OwnerId,
            OwnerName = ProjectMemberNameResolver.ResolveUserName(userDict, package.OwnerId),
            Files = new List<ProjectFileWeb>(),
            TotalFiles = totalFiles,
            ParentId = package.ParentId
        };
    }
}
