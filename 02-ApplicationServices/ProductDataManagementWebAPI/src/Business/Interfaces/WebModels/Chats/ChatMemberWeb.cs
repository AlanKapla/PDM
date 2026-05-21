namespace Business.Interfaces.WebModels.Chats;

/// <summary>
/// API-facing DTO for a chat member.
/// </summary>
public sealed record ChatMemberWeb(
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime JoinedAt,
    bool IsAdmin,
    DateTime? LastReadAt);
