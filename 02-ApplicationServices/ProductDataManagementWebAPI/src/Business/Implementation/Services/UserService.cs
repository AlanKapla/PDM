using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Serwis zarządzający użytkownikami z globalnym cachowaniem
/// Cache jest shared dla całego systemu - YOLO style
/// </summary>
public sealed class UserService : IUserService
{
    private readonly ICacheService cacheService;
    private readonly IReadRepository<User> userRepository;
    private readonly ILogger<UserService> logger;

    public UserService(
        ICacheService cacheService,
        IReadRepository<User> userRepository,
        ILogger<UserService> logger)
    {
        this.cacheService = cacheService;
        this.userRepository = userRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Pobiera wszystkich użytkowników z cache jako słownik [UserId -> User]
    /// Dane są cachowane globalnie dla całego systemu
    /// </summary>
    public async Task<Dictionary<Guid, User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        string cacheKey = "users:all";

        Dictionary<Guid, User>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug("Loading all users from database");

                IEnumerable<User> allUsers = await userRepository.GetAll();

                Dictionary<Guid, User> userDict = allUsers.ToDictionary(u => u.Id);

                logger.LogInformation("Cached {Count} users globally", userDict.Count);

                return userDict;
            },
            expiration: TimeSpan.FromHours(1), // Long TTL - users don't change often
            cancellationToken: cancellationToken
        );

        return result ?? new Dictionary<Guid, User>();
    }

    /// <summary>
    /// Pobiera pojedynczego użytkownika z cache
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, User> allUsers = await GetAllUsersAsync(cancellationToken);
        return allUsers.GetValueOrDefault(userId);
    }

    /// <summary>
    /// Invaliduje cache użytkowników (np. po synchronizacji z Azure AD B2C)
    /// </summary>
    public async Task InvalidateUsersCacheAsync(CancellationToken cancellationToken = default)
    {
        string cacheKey = "users:all";
        await cacheService.RemoveCacheByKeyAsync(cacheKey, cancellationToken);

        logger.LogInformation("Invalidated global users cache");
    }
}
