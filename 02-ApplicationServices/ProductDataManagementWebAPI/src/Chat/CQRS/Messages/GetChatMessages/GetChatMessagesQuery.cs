using Business.Interfaces.WebModels.Chats;
using CQRS;

namespace Chat.CQRS.Messages.GetChatMessages;

/// <summary>
/// Returns cursor-paginated messages for a chat, ordered newest → oldest.
/// Pass <c>Before</c> as the Id of the oldest message already held by the client
/// to load the next page. Omit it to receive the most recent page.
/// The requesting user must be a member of the chat.
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> is optional: tenant-scoped controllers pass it from the
/// route, while the direct-chats controller leaves it null. Authorization for
/// the tenant scope is enforced at the controller via the <c>[Authorize(Policy)]</c>
/// attribute; this query intentionally does not implement <c>IAuthorizableRequest</c>
/// so it can flow through both routes.
/// </remarks>
public sealed record GetChatMessagesQuery : IRequestQuery<List<MessageWeb>>
{
    public Guid? TenantId { get; init; }
    public required Guid ChatId { get; init; }
    public Guid? Before { get; init; }
    public int PageSize { get; init; } = 50;
}
