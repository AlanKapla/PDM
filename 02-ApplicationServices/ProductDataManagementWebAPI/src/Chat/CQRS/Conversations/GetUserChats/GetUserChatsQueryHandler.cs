using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Chat.DTOs;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.GetUserChats;

public sealed class GetUserChatsQueryHandler : IRequestHandler<GetUserChatsQuery, List<ChatWeb>>
{
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IReadRepository<MessageHistory> messageRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public GetUserChatsQueryHandler(
        IReadRepository<ChatMember> chatMemberRepo,
        IReadRepository<MessageHistory> messageRepo,
        IProjectMemberService projectMemberService,
        ICurrentUser currentUser)
    {
        this.chatMemberRepo = chatMemberRepo;
        this.messageRepo = messageRepo;
        this.projectMemberService = projectMemberService;
        this.currentUser = currentUser;
    }

    public async Task<List<ChatWeb>> Handle(GetUserChatsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<ChatMember> userMemberships = await chatMemberRepo.GetBySearch(
            cm => cm.UserId == currentUser.Id,
            include => include
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Members));

        if (!userMemberships.Any())
        {
            return new List<ChatWeb>();
        }

        List<Guid> chatIds = userMemberships.Select(cm => cm.ChatId).ToList();

        IEnumerable<MessageHistory> lastMessages = await messageRepo.GetBySearch(
            m => chatIds.Contains(m.ChatId) && m.DeletedAt == null);

        Dictionary<Guid, MessageHistory?> lastMessageByChat = chatIds.ToDictionary(
            id => id,
            id => lastMessages
                .Where(m => m.ChatId == id)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault());

        HashSet<Guid> allUserIds = userMemberships
            .SelectMany(cm => cm.Chat.Members.Select(m => m.UserId))
            .Concat(lastMessages.Select(m => m.UserId))
            .ToHashSet();

        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(allUserIds, cancellationToken);

        List<ChatWeb> result = userMemberships
            .OrderByDescending(cm => lastMessageByChat.TryGetValue(cm.ChatId, out MessageHistory? lm)
                ? lm?.CreatedAt ?? cm.Chat.CreatedAt
                : cm.Chat.CreatedAt)
            .Select(cm =>
            {
                var chat = cm.Chat;
                MessageHistory? lastMsg = lastMessageByChat.GetValueOrDefault(chat.Id);

                int unreadCount = lastMessages
                    .Count(m =>
                        m.ChatId == chat.Id &&
                        m.UserId != currentUser.Id &&
                        m.DeletedAt == null &&
                        (cm.LastReadAt == null || m.CreatedAt > cm.LastReadAt));

                List<ChatMemberWeb> members = chat.Members
                    .Select(m =>
                    {
                        userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) user);
                        return new ChatMemberWeb(
                            UserId: m.UserId,
                            FirstName: user.FirstName ?? string.Empty,
                            LastName: user.LastName ?? string.Empty,
                            JoinedAt: m.JoinedAt,
                            IsAdmin: m.IsAdmin,
                            LastReadAt: m.LastReadAt);
                    })
                    .ToList();

                return new ChatWeb(
                    Id: chat.Id,
                    Name: chat.Name,
                    IsGroupChat: chat.IsGroupChat,
                    ProjectId: chat.ProjectId,
                    TenantId: chat.TenantId,
                    CreatedAt: chat.CreatedAt,
                    CreatedByUserId: chat.CreatedByUserId,
                    UnreadCount: unreadCount,
                    LastMessage: lastMsg != null ? MapLastMessage(lastMsg, userNames) : null,
                    Members: members);
            })
            .ToList();

        return result;
    }

    private static MessageWeb MapLastMessage(
        MessageHistory m,
        Dictionary<Guid, (string FirstName, string LastName)> userNames)
    {
        userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) sender);
        return new MessageWeb(
            Id: m.Id,
            ChatId: m.ChatId,
            SenderId: m.UserId,
            SenderFirstName: sender.FirstName ?? string.Empty,
            SenderLastName: sender.LastName ?? string.Empty,
            Content: m.IsDeleted ? string.Empty : m.Content,
            IsDeleted: m.IsDeleted,
            IsEdited: m.EditedAt.HasValue,
            SentAt: m.CreatedAt,
            EditedAt: m.EditedAt,
            ReplyToMessageId: m.ReplyToMessageId);
    }
}
