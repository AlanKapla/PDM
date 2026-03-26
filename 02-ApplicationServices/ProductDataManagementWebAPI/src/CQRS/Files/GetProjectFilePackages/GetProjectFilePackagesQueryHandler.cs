using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using MediatR;

namespace CQRS.Files.GetProjectFilePackages;

public class GetProjectFilePackagesQueryHandler : IRequestHandler<GetProjectFilePackagesQuery, List<ProjectFilePackageWeb>>
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
            string ownerName = string.Empty;
            if (userDict.TryGetValue(package.OwnerId, out ProjectMemberUserInfo? owner))
            {
                ownerName = owner.FullName;
            }

            result.Add(new ProjectFilePackageWeb
            {
                Id = package.Id,
                Name = package.Name,
                CreatedAt = package.CreatedAt,
                OwnerId = package.OwnerId,
                OwnerName = ownerName,
                Files = new List<ProjectFileWeb>(),
                TotalFiles = fileCountDict.GetValueOrDefault(packageId, 0)
            });
        }

        result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        return result;
    }
}
