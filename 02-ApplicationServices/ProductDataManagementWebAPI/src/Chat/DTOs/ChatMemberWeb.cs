namespace Chat.DTOs;

/// <summary>
/// API-facing DTO for a chat member.
/// </summary>
public record ChatMemberWeb(
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime JoinedAt,
    bool IsAdmin,
    DateTime? LastReadAt);
