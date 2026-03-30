using Chat.DTOs;
using CQRS;

namespace Chat.CQRS.Conversations.CreateChat;

/// <summary>
/// Creates a chat. If MemberUserIds has exactly 1 entry, creates or returns an existing direct chat
/// (ProjectId is ignored). If MemberUserIds has 2 or more entries, creates a new group chat bound
/// to ProjectId. The caller is automatically included in all cases.
/// If Name is null, falls back to a comma-separated list of member display names.
/// </summary>
public sealed record CreateChatCommand(
    Guid? ProjectId,
    List<Guid> MemberUserIds,
    string? Name = null) : IRequestCommand<CreateChatResultWeb>;
