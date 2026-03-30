namespace Chat.Services;

public interface IChatDirectService
{
    /// <summary>
    /// Finds an existing direct chat between two users, or creates one if none exists.
    /// Notifies the non-requesting user via SignalR.
    /// Returns the direct chat ID.
    /// </summary>
    Task<Guid> EnsureDirectChatAsync(
        Guid userA,
        Guid userB,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}
