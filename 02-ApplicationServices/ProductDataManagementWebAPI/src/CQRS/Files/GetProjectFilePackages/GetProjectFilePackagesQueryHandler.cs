using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetProjectFilePackages;

public class GetProjectFilePackagesQueryHandler : IRequestHandler<GetProjectFilePackagesQuery, List<ProjectFilePackageWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IReadRepository<User> userRepository;
    private readonly ICurrentUser currentUser;

    public GetProjectFilePackagesQueryHandler(
        IProjectFilesService projectFilesService,
        IReadRepository<User> userRepository,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.userRepository = userRepository;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFilePackageWeb>> Handle(GetProjectFilePackagesQuery request, CancellationToken cancellationToken)
    {
        // Pobierz cachowane dane z ProjectFilesService
        Dictionary<Guid, ProjectFilePackageDto> allPackages = await projectFilesService.GetProjectFilePackagesAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        Dictionary<Guid, List<ProjectFileCacheDto>> allFiles = await projectFilesService.GetProjectPackageFilesAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        if (allPackages.Count == 0)
        {
            return new List<ProjectFilePackageWeb>();
        }

        // Filtruj paczki według scope
        HashSet<Guid> accessiblePackageIds = await GetAccessiblePackageIdsByScopeAsync(
            request.TenantId,
            request.ProjectId,
            request.Scope,
            allPackages,
            cancellationToken);

        if (accessiblePackageIds.Count == 0)
        {
            return new List<ProjectFilePackageWeb>();
        }

        // Policz pliki dla każdej paczki według scope
        Dictionary<Guid, int> fileCountDict = await GetFileCountsByScopeAsync(
            accessiblePackageIds,
            allFiles,
            request.Scope,
            cancellationToken);

        // Zbierz unikalne OwnerId i pobierz użytkowników jednym zapytaniem jako słownik
        HashSet<Guid> ownerIds = accessiblePackageIds
            .Where(packageId => allPackages.ContainsKey(packageId))
            .Select(packageId => allPackages[packageId].OwnerId)
            .ToHashSet();

        Dictionary<Guid, User> userDict = new Dictionary<Guid, User>();

        if (ownerIds.Count > 0)
        {
            userDict = await userRepository.GetDictionaryBySearchAsync(
                u => ownerIds.Contains(u.Id),
                cancellationToken);
        }

        // Buduj wynik
        List<ProjectFilePackageWeb> result = new List<ProjectFilePackageWeb>();

        foreach (Guid packageId in accessiblePackageIds)
        {
            if (!allPackages.TryGetValue(packageId, out ProjectFilePackageDto? package))
            {
                continue;
            }

            // Pobierz nazwę właściciela ze słownika
            string ownerName = string.Empty;
            if (userDict.TryGetValue(package.OwnerId, out User? owner))
            {
                ownerName = $"{owner.FirstName} {owner.LastName}".Trim();
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

        // Sortuj według daty utworzenia (najnowsze pierwsze)
        result.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        return result;
    }

    private async Task<HashSet<Guid>> GetAccessiblePackageIdsByScopeAsync(
        Guid tenantId,
        Guid projectId,
        ResourceScope scope,
        Dictionary<Guid, ProjectFilePackageDto> allPackages,
        CancellationToken cancellationToken)
    {
        if (scope == ResourceScope.All)
        {
            return allPackages.Keys.ToHashSet();
        }

        if (scope == ResourceScope.Mine)
        {
            return allPackages.Values
                .Where(p => p.OwnerId == currentUser.Id)
                .Select(p => p.Id)
                .ToHashSet();
        }

        // ResourceScope.Shared - użyj ProjectFilesService
        return await projectFilesService.GetAccessiblePackageIdsAsync(
            currentUser,
            projectId,
            scope,
            cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> GetFileCountsByScopeAsync(
        HashSet<Guid> packageIds,
        Dictionary<Guid, List<ProjectFileCacheDto>> allFiles,
        ResourceScope scope,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, int> counts = new Dictionary<Guid, int>();

        if (scope == ResourceScope.All)
        {
            foreach (Guid packageId in packageIds)
            {
                int count = allFiles.TryGetValue(packageId, out List<ProjectFileCacheDto>? files)
                    ? files.Count
                    : 0;
                counts[packageId] = count;
            }
        }
        else if (scope == ResourceScope.Mine)
        {
            foreach (Guid packageId in packageIds)
            {
                int count = allFiles.TryGetValue(packageId, out List<ProjectFileCacheDto>? files)
                    ? files.Count(f => f.OwnerId == currentUser.Id)
                    : 0;
                counts[packageId] = count;
            }
        }
        else // ResourceScope.Shared
        {
            counts = await projectFilesService.GetAccessibleFileCountsAsync(
                currentUser,
                packageIds,
                scope,
                cancellationToken);
        }

        return counts;
    }
}
