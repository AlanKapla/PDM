namespace Chat;

/// <summary>
/// Configuration options for the Chat module.
/// Bind from appsettings.json section "Chat".
/// </summary>
public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    /// <summary>
    /// Maximum time window in minutes within which a message author can edit their message.
    /// </summary>
    public int MaxMessageEditWindowMinutes { get; init; } = 15;

    /// <summary>
    /// Number of messages returned per page in GetChatMessages.
    /// </summary>
    public int MessagePageSize { get; init; } = 50;

    public TimeSpan MaxEditWindow => TimeSpan.FromMinutes(MaxMessageEditWindowMinutes);
}
