namespace Chat.DTOs;

/// <summary>
/// Result of a chat search operation.
/// MatchingMessageIds contains IDs of messages whose content matched the search phrase.
/// Empty when the match was on chat name or member name only.
/// </summary>
public record ChatSearchResultWeb(
    Guid ChatId,
    string ChatName,
    bool IsGroupChat,
    Guid? ProjectId,
    Guid? TenantId,
    List<Guid> MatchingMessageIds);
