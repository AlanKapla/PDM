using Entities.Models;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class PermissionsVersionService
{
    private readonly IRepository<PermissionsVersionProfile> permissionsVersionRepo;
    private readonly ILogger<PermissionsVersionService> logger;

    public PermissionsVersionService(
        IRepository<PermissionsVersionProfile> permissionsVersionRepo,
        ILogger<PermissionsVersionService> logger)
    {
        this.permissionsVersionRepo = permissionsVersionRepo;
        this.logger = logger;
    }

    public async Task BumpVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await permissionsVersionRepo.GetFirstBySearch(
            p => p.UserId == userId);

        if (profile == null)
        {
            profile = new PermissionsVersionProfile
            {
                UserId = userId,
                Version = 2
            };
            await permissionsVersionRepo.Insert(profile);
            logger.LogInformation("Created PermissionsVersion profile for user {UserId} with version 2", userId);
        }
        else
        {
            profile.Version++;
            await permissionsVersionRepo.Update(profile);
            logger.LogInformation("Bumped PermissionsVersion for user {UserId} to version {Version}", userId, profile.Version);
        }
    }

    public async Task BumpVersionsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            await BumpVersionAsync(userId, cancellationToken);
        }
    }
}
