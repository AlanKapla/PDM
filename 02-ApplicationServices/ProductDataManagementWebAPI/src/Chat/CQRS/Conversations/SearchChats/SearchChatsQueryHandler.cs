using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Chats;
using Entities.Models.Chats;
using Entities.Models.Users;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.SearchChats;

public sealed class SearchChatsQueryHandler : IRequestHandler<SearchChatsQuery, List<ChatSearchResultWeb>>
{
    private const int MaxResults = 50;

    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IReadRepository<MessageHistory> messageRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly ICurrentUser currentUser;

    public SearchChatsQueryHandler(
        IReadRepository<ChatModel> chatRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IReadRepository<MessageHistory> messageRepo,
        IReadRepository<User> userRepo,
        ICurrentUser currentUser)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.messageRepo = messageRepo;
        this.userRepo = userRepo;
        this.currentUser = currentUser;
    }

    public async Task<List<ChatSearchResultWeb>> Handle(SearchChatsQuery request, CancellationToken cancellationToken)
    {
        List<Guid> myChatIds = await chatMemberRepo.SelectAsync(
            cm => cm.UserId == currentUser.Id,
            cm => cm.ChatId,
            cancellationToken);

        if (myChatIds.Count == 0)
        {
            return new();
        }

        string phrase = request.Phrase.Trim();

        // My chats (optionally restricted to a tenant) — projected to flat rows.
        List<ChatHeader> chatHeaders = request.TenantId is not null
            ? await chatRepo.SelectAsync(
                c => myChatIds.Contains(c.Id) && c.TenantId == request.TenantId.Value,
                c => new ChatHeader(c.Id, c.Name, c.IsGroupChat, c.ProjectId, c.TenantId),
                cancellationToken)
            : await chatRepo.SelectAsync(
                c => myChatIds.Contains(c.Id),
                c => new ChatHeader(c.Id, c.Name, c.IsGroupChat, c.ProjectId, c.TenantId),
                cancellationToken);

        if (chatHeaders.Count == 0)
        {
            return new();
        }

        List<Guid> scopedChatIds = chatHeaders.Select(c => c.Id).ToList();

        // Matching message ids per chat — single SQL projection (no full entities).
        List<MessageMatch> matchingMessages = await messageRepo.SelectAsync(
            m => scopedChatIds.Contains(m.ChatId)
                 && m.DeletedAt == null
                 && m.Content.Contains(phrase),
            m => new MessageMatch(m.ChatId, m.Id),
            cancellationToken);

        Dictionary<Guid, List<Guid>> messageIdsByChatId = matchingMessages
            .GroupBy(m => m.ChatId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.MessageId).ToList());

        // User name match pushed to SQL via JOIN through ChatMember → User.
        HashSet<Guid> matchingUserIds = await userRepo.SelectToHashSetAsync(
            u => (u.FirstName + " " + u.LastName).Contains(phrase),
            u => u.Id,
            cancellationToken);

        HashSet<Guid> chatsWithMemberNameMatch = matchingUserIds.Count == 0
            ? new HashSet<Guid>()
            : await chatMemberRepo.SelectToHashSetAsync(
                cm => scopedChatIds.Contains(cm.ChatId) && matchingUserIds.Contains(cm.UserId),
                cm => cm.ChatId,
                cancellationToken);

        return chatHeaders
            .Where(c =>
                c.Name.Contains(phrase, StringComparison.OrdinalIgnoreCase) ||
                messageIdsByChatId.ContainsKey(c.Id) ||
                chatsWithMemberNameMatch.Contains(c.Id))
            .Take(MaxResults)
            .Select(c => new ChatSearchResultWeb(
                ChatId: c.Id,
                ChatName: c.Name,
                IsGroupChat: c.IsGroupChat,
                ProjectId: c.ProjectId,
                TenantId: c.TenantId,
                MatchingMessageIds: messageIdsByChatId.GetValueOrDefault(c.Id) ?? new()))
            .ToList();
    }

    private sealed record ChatHeader(Guid Id, string Name, bool IsGroupChat, Guid? ProjectId, Guid? TenantId);

    private sealed record MessageMatch(Guid ChatId, Guid MessageId);
}
