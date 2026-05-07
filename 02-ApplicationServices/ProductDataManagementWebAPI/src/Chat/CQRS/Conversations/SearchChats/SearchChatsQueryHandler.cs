using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Chat.DTOs;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.SearchChats;

public sealed class SearchChatsQueryHandler : IRequestHandler<SearchChatsQuery, List<ChatSearchResultWeb>>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IReadRepository<MessageHistory> messageRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public SearchChatsQueryHandler(
        IReadRepository<ChatModel> chatRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IReadRepository<MessageHistory> messageRepo,
        IProjectMemberService projectMemberService,
        ICurrentUser currentUser)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.messageRepo = messageRepo;
        this.projectMemberService = projectMemberService;
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

        // Load my chats, messages matching the phrase, and all member entries
        IEnumerable<ChatModel> allMyChats = await chatRepo.GetBySearch(
            c => myChatIds.Contains(c.Id));

        IEnumerable<MessageHistory> matchingMessages = await messageRepo.GetBySearch(
            m => myChatIds.Contains(m.ChatId) && m.DeletedAt == null && m.Content.Contains(phrase));

        IEnumerable<ChatMember> allMembers = await chatMemberRepo.GetBySearch(
            cm => myChatIds.Contains(cm.ChatId));

        // Resolve member names for name-based matching
        HashSet<Guid> memberUserIds = allMembers.Select(m => m.UserId).ToHashSet();
        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(memberUserIds, cancellationToken);

        // chatId → list of matching message IDs
        Dictionary<Guid, List<Guid>> messageIdsByChatId = matchingMessages
            .GroupBy(m => m.ChatId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.Id).ToList());

        // chatId → true if any member's full name contains the phrase
        HashSet<Guid> chatsWithMemberNameMatch = allMembers
            .GroupBy(m => m.ChatId)
            .Where(g => g.Any(m =>
            {
                userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) name);
                return $"{name.FirstName} {name.LastName}".Trim()
                    .Contains(phrase, StringComparison.OrdinalIgnoreCase);
            }))
            .Select(g => g.Key)
            .ToHashSet();

        return allMyChats
            .Where(c =>
                c.Name.Contains(phrase, StringComparison.OrdinalIgnoreCase) ||
                messageIdsByChatId.ContainsKey(c.Id) ||
                chatsWithMemberNameMatch.Contains(c.Id))
            .Select(c => new ChatSearchResultWeb(
                ChatId: c.Id,
                ChatName: c.Name,
                IsGroupChat: c.IsGroupChat,
                ProjectId: c.ProjectId,
                TenantId: c.TenantId,
                MatchingMessageIds: messageIdsByChatId.GetValueOrDefault(c.Id) ?? new()))
            .ToList();
    }
}
