using Business.Interfaces.WebModels.Chats;
using CQRS;

namespace Chat.CQRS.Conversations.GetUserChats;

/// <summary>
/// Returns chats for the current user.
/// When <paramref name="TenantId"/> is set, only chats belonging to that tenant are returned
/// (used by the tenant-scoped controller). When it is null and
/// <paramref name="DirectChatsOnly"/> is true, only direct chats (cross-tenant) are returned
/// (used by the direct-chats controller).
/// </summary>
public sealed record GetUserChatsQuery(Guid? TenantId = null, bool DirectChatsOnly = false)
    : IRequestQuery<List<ChatWeb>>;
