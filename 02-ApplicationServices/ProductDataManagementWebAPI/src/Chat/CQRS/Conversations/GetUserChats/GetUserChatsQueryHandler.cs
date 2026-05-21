using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using Chat.Mappers;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
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
        IEnumerable<ChatMember> userMembershipsRaw = await chatMemberRepo.GetBySearch(
            cm => cm.UserId == currentUser.Id,
            include => include
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Members));

        // Tenant-scoped: restrict to chats belonging to the requested tenant.
        // Direct-only: restrict to chats with no tenant (cross-tenant direct chats).
        List<ChatMember> userMemberships;
        if (request.TenantId is not null)
        {
            userMemberships = userMembershipsRaw
                .Where(cm => cm.Chat.TenantId == request.TenantId.Value)
                .ToList();
        }
        else if (request.DirectChatsOnly)
        {
            userMemberships = userMembershipsRaw
                .Where(cm => cm.Chat.TenantId == null && !cm.Chat.IsGroupChat)
                .ToList();
        }
        else
        {
            userMemberships = userMembershipsRaw.ToList();
        }

        if (userMemberships.Count == 0)
        {
            return new List<ChatWeb>();
        }

        List<Guid> chatIds = userMemberships.Select(cm => cm.ChatId).ToList();

        // Last message per chat — single SQL query (GROUP BY ChatId + correlated First).
        List<MessageHistory> lastMessages = await messageRepo.SelectGroupedAsync(
            m => chatIds.Contains(m.ChatId) && m.DeletedAt == null,
            m => m.ChatId,
            g => g.OrderByDescending(m => m.CreatedAt).First(),
            cancellationToken);

        Dictionary<Guid, MessageHistory> lastMessageByChat = lastMessages
            .ToDictionary(m => m.ChatId);

        // Unread count per chat — single SQL query joining ChatMember.LastReadAt.
        Guid currentUserId = currentUser.Id;
        List<UnreadProjection> unreadProjections = await chatMemberRepo.SelectAsync(
            cm => cm.UserId == currentUserId && chatIds.Contains(cm.ChatId),
            cm => new UnreadProjection
            {
                ChatId = cm.ChatId,
                Count = cm.Chat.Messages.Count(m =>
                    m.UserId != currentUserId
                    && m.DeletedAt == null
                    && (cm.LastReadAt == null || m.CreatedAt > cm.LastReadAt))
            },
            cancellationToken);

        Dictionary<Guid, int> unreadByChatId = unreadProjections
            .ToDictionary(u => u.ChatId, u => u.Count);

        HashSet<Guid> allUserIds = userMemberships
            .SelectMany(cm => cm.Chat.Members.Select(m => m.UserId))
            .Concat(lastMessages.Select(m => m.UserId))
            .ToHashSet();

        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(allUserIds, cancellationToken);

        List<ChatWeb> result = userMemberships
            .OrderByDescending(cm => lastMessageByChat.TryGetValue(cm.ChatId, out MessageHistory? lm)
                ? lm.CreatedAt
                : cm.Chat.CreatedAt)
            .Select(cm =>
            {
                ChatModel chat = cm.Chat;
                MessageHistory? lastMsg = lastMessageByChat.GetValueOrDefault(chat.Id);
                int unreadCount = unreadByChatId.GetValueOrDefault(chat.Id);

                List<ChatMemberWeb> members = chat.Members
                    .Select(m =>
                    {
                        userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) user);
                        return ChatMapper.MapMember(m, user.FirstName, user.LastName);
                    })
                    .ToList();

                MessageWeb? lastMessageWeb = lastMsg != null ? MapLastMessage(lastMsg, userNames) : null;
                return ChatMapper.MapChat(chat, members, lastMessageWeb, unreadCount);
            })
            .ToList();

        return result;
    }

    private sealed class UnreadProjection
    {
        public Guid ChatId { get; init; }
        public int Count { get; init; }
    }

    private static MessageWeb MapLastMessage(
        MessageHistory m,
        Dictionary<Guid, (string FirstName, string LastName)> userNames)
    {
        userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) sender);
        return ChatMapper.MapMessage(m, sender.FirstName, sender.LastName);
    }
}
