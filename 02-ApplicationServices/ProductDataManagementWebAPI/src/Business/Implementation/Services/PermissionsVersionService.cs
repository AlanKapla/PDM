using Business.Interfaces.Services;
using Entities.Models;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class PermissionsVersionService : IPermissionsVersionService
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
        List<Guid> userIdList = userIds.ToList();
        if (userIdList.Count == 0)
        {
            return;
        }

        await permissionsVersionRepo.ExecuteUpdateAsync(
            p => userIdList.Contains(p.UserId),
            p => p.SetProperty(x => x.Version, x => x.Version + 1),
            cancellationToken);

        HashSet<Guid> existingUserIds = await permissionsVersionRepo.SelectToHashSetAsync(
            p => userIdList.Contains(p.UserId),
            p => p.UserId,
            cancellationToken);

        List<PermissionsVersionProfile> newProfiles = userIdList
            .Where(id => !existingUserIds.Contains(id))
            .Select(id => new PermissionsVersionProfile { UserId = id, Version = 2 })
            .ToList();

        if (newProfiles.Count > 0)
        {
            await permissionsVersionRepo.InsertRange(newProfiles);
            await permissionsVersionRepo.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created {Count} new PermissionsVersion profiles", newProfiles.Count);
        }
    }
}
