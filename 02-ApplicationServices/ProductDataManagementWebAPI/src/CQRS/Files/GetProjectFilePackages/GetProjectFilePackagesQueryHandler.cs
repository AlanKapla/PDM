using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetProjectFilePackages;

public class GetProjectFilePackagesQueryHandler : IRequestHandler<GetProjectFilePackagesQuery, List<ProjectFilePackageWeb>>
{
    private readonly IRepository<ProjectFilePackage> packageRepo;
    private readonly IRepository<ProjectFile> fileRepo;
    private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
    private readonly ICurrentUser currentUser;

    public GetProjectFilePackagesQueryHandler(
        IRepository<ProjectFilePackage> packageRepo,
        IRepository<ProjectFile> fileRepo,
        IRepository<SharedProjectFile> sharedProjectFileRepo,
        ICurrentUser currentUser)
    {
        this.packageRepo = packageRepo;
        this.fileRepo = fileRepo;
        this.sharedProjectFileRepo = sharedProjectFileRepo;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFilePackageWeb>> Handle(GetProjectFilePackagesQuery request, CancellationToken cancellationToken)
    {
        // Get packages directly based on scope
        var packages = await GetPackagesForScopeAsync(request.TenantId, request.ProjectId, request.Scope);
        
        if (packages.Count == 0)
        {
            return new List<ProjectFilePackageWeb>();
        }

        var packageIds = packages.Select(p => p.Id).ToHashSet();

        var fileCountDict = await fileRepo.CountGroupedByAsync(
            pf => packageIds.Contains(pf.ProjectFilePackageId) && !pf.IsDeleted,
            pf => pf.ProjectFilePackageId,
            cancellationToken);

        // Sort once
        packages.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        var result = new List<ProjectFilePackageWeb>(packages.Count);
        foreach (var pkg in packages)
        {
            result.Add(new ProjectFilePackageWeb
            {
                Id = pkg.Id,
                Name = pkg.Name,
                CreatedAt = pkg.CreatedAt,
                OwnerId = pkg.OwnerId,
                OwnerName = $"{pkg.Owner.FirstName} {pkg.Owner.LastName}".Trim(),
                Files = new List<ProjectFileWeb>(),
                TotalFiles = fileCountDict.GetValueOrDefault(pkg.Id, 0)
            });
        }

        return result;
    }

    private async Task<List<ProjectFilePackage>> GetPackagesForScopeAsync(
        Guid tenantId,
        Guid projectId,
        ResourceScope scope)
    {
        return scope switch
        {
            ResourceScope.Mine => await GetMyPackagesAsync(tenantId, projectId),
            ResourceScope.Shared => await GetSharedPackagesAsync(tenantId, projectId),
            ResourceScope.All => await GetAllPackagesAsync(tenantId, projectId),
            _ => new List<ProjectFilePackage>()
        };
    }

    private async Task<List<ProjectFilePackage>> GetMyPackagesAsync(Guid tenantId, Guid projectId)
    {
        var packages = await packageRepo.GetBySearch(
            pfp => pfp.ProjectId == projectId &&
                   pfp.TenantId == tenantId &&
                   pfp.OwnerId == currentUser.Id &&
                   !pfp.IsDeleted,
            include => include.Include(pfp => pfp.Owner)
        );
        return packages.ToList();
    }

    private async Task<List<ProjectFilePackage>> GetSharedPackagesAsync(Guid tenantId, Guid projectId)
    {
        // Get package IDs from shared files
        var packageIds = await sharedProjectFileRepo.SelectToHashSetAsync(
            spf => spf.ProjectId == projectId &&
                   spf.TenantId == tenantId &&
                   spf.SharedWithUserId == currentUser.Id &&
                   !spf.ProjectFile.IsDeleted,
            spf => spf.ProjectFile.ProjectFilePackageId
        );

        if (packageIds.Count == 0)
        {
            return new List<ProjectFilePackage>();
        }

        // Get packages
        var packages = await packageRepo.GetBySearch(
            pfp => packageIds.Contains(pfp.Id) && !pfp.IsDeleted,
            include => include.Include(pfp => pfp.Owner)
        );
        return packages.ToList();
    }

    private async Task<List<ProjectFilePackage>> GetAllPackagesAsync(Guid tenantId, Guid projectId)
    {
        var packages = await packageRepo.GetBySearch(
            pfp => pfp.ProjectId == projectId &&
                   pfp.TenantId == tenantId &&
                   !pfp.IsDeleted,
            include => include.Include(pfp => pfp.Owner)
        );
        return packages.ToList();
    }
}
