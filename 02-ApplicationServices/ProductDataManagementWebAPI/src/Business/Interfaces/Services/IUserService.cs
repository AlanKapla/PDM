using Entities.Models;

namespace Business.Interfaces.Services;

/// <summary>
/// Serwis zarządzający użytkownikami z globalnym cachowaniem
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Pobiera wszystkich użytkowników z cache jako słownik [UserId -> User]
    /// Dane są cachowane globalnie dla całego systemu
    /// </summary>
    Task<Dictionary<Guid, User>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera pojedynczego użytkownika z cache
    /// </summary>
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invaliduje cache użytkowników (np. po synchronizacji z Azure AD B2C)
    /// </summary>
    Task InvalidateUsersCacheAsync(CancellationToken cancellationToken = default);
}
