using Business.Interfaces.WebModels.Chats;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;

namespace Chat.Mappers;

/// <summary>
/// Maps Chat domain entities to their API-facing Web DTOs.
/// </summary>
public static class ChatMapper
{
    public static MessageWeb MapMessage(MessageHistory message, string senderFirstName, string senderLastName)
    {
        return new MessageWeb(
            Id: message.Id,
            ChatId: message.ChatId,
            SenderId: message.UserId,
            SenderFirstName: senderFirstName ?? string.Empty,
            SenderLastName: senderLastName ?? string.Empty,
            Content: message.IsDeleted ? string.Empty : message.Content,
            IsDeleted: message.IsDeleted,
            IsEdited: message.EditedAt.HasValue,
            SentAt: message.CreatedAt,
            EditedAt: message.EditedAt,
            ReplyToMessageId: message.ReplyToMessageId);
    }

    public static ChatMemberWeb MapMember(ChatMember member, string firstName, string lastName)
    {
        return new ChatMemberWeb(
            UserId: member.UserId,
            FirstName: firstName ?? string.Empty,
            LastName: lastName ?? string.Empty,
            JoinedAt: member.JoinedAt,
            IsAdmin: member.IsAdmin,
            LastReadAt: member.LastReadAt);
    }

    public static ChatWeb MapChat(
        ChatModel chat,
        IReadOnlyCollection<ChatMemberWeb> members,
        MessageWeb? lastMessage,
        int unreadCount)
    {
        return new ChatWeb(
            Id: chat.Id,
            Name: chat.Name,
            IsGroupChat: chat.IsGroupChat,
            ProjectId: chat.ProjectId,
            TenantId: chat.TenantId,
            CreatedAt: chat.CreatedAt,
            CreatedByUserId: chat.CreatedByUserId,
            UnreadCount: unreadCount,
            LastMessage: lastMessage,
            Members: members.ToList());
    }
}
