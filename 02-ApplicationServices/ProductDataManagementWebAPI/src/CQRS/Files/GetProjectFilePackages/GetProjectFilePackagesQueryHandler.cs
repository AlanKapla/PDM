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

        List<ProjectFilePackageWeb> result = new List<ProjectFilePackageWeb>();

        foreach ((Guid packageId, ProjectFilePackageDto package) in accessiblePackages)
        {
            int totalFiles = fileCountDict.GetValueOrDefault(packageId, 0);
            result.Add(MapToPackageWeb(package, userDict, totalFiles));
        }

        result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        return result;
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
            TotalFiles = totalFiles
        };
    }
}
