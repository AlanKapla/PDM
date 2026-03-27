namespace Chat.Hubs;

internal static class ChatHubGroups
{
    internal static string Chat(Guid chatId) => $"chat:{chatId}";
    internal static string User(Guid userId) => $"user:{userId}";
}
