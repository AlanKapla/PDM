using Chat.DTOs;

namespace Chat.Hubs;

/// <summary>
/// Typed client interface for ChatHub.
/// All Server→Client events are defined here.
/// </summary>
public interface IChatClient
{
    /// <summary>New message received in a conversation.</summary>
    Task ReceiveMessage(MessageWeb message);

    /// <summary>A message was edited by its author.</summary>
    Task MessageEdited(MessageEditedPayload payload);

    /// <summary>A message was soft-deleted.</summary>
    Task MessageDeleted(MessageDeletedPayload payload);

    /// <summary>Typing indicator from another member.</summary>
    Task UserTyping(UserTypingPayload payload);

    /// <summary>Another member marked the conversation as read.</summary>
    Task ReadReceipt(ReadReceiptPayload payload);

    /// <summary>Current user was added to a new conversation.</summary>
    Task ChatCreated(ChatWeb chat);

    /// <summary>Current user was removed from a conversation.</summary>
    Task RemovedFromChat(RemovedFromChatPayload payload);

    /// <summary>A new member was added to a group chat.</summary>
    Task MemberAdded(MemberAddedPayload payload);

    /// <summary>A group chat was dissolved by its admin.</summary>
    Task ChatDeleted(Guid chatId);
}

public record MessageEditedPayload(Guid MessageId, Guid ChatId, string NewContent, DateTime EditedAt);
public record MessageDeletedPayload(Guid MessageId, Guid ChatId);
public record UserTypingPayload(Guid ChatId, Guid UserId, bool IsTyping);
public record ReadReceiptPayload(Guid ChatId, Guid UserId, DateTime ReadAt);
public record RemovedFromChatPayload(Guid ChatId, Guid? RedirectToChatId);
public record MemberAddedPayload(Guid ChatId, ChatMemberWeb Member);
