namespace Business.Interfaces.WebModels.Chats.Requests;

/// <summary>
/// Request body for <c>POST /api/chats/direct</c>.
/// Creates a 1-1 direct chat with the target user (idempotent).
/// </summary>
public sealed record CreateDirectChatRequest
{
    public required Guid TargetUserId { get; init; }
}
