using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using Chat.Hubs;
using Chat.Mappers;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using ChatModel = Entities.Models.Chats.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.Services;

public sealed class ChatDirectService : IChatDirectService
{
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ILogger<ChatDirectService> logger;

    public ChatDirectService(
        IRepository<ChatMember> chatMemberRepo,
        IRepository<ChatModel> chatWriteRepo,
        IProjectMemberService projectMemberService,
        IHubContext<ChatHub, IChatClient> hubContext,
        ILogger<ChatDirectService> logger)
    {
        this.chatMemberRepo = chatMemberRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.projectMemberService = projectMemberService;
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public async Task<Guid> EnsureDirectChatAsync(
        Guid userA,
        Guid userB,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> userAChatIds = await chatMemberRepo.SelectAsync(
            cm => cm.UserId == userA,
            cm => cm.ChatId,
            cancellationToken);

        if (userAChatIds.Count > 0)
        {
            List<Guid> directChatIds = await chatWriteRepo.SelectAsync(
                c => userAChatIds.Contains(c.Id) && !c.IsGroupChat,
                c => c.Id,
                cancellationToken);

            if (directChatIds.Count > 0)
            {
                Guid existingChatId = (await chatMemberRepo.SelectAsync(
                    cm => cm.UserId == userB && directChatIds.Contains(cm.ChatId),
                    cm => cm.ChatId,
                    cancellationToken)).FirstOrDefault();

                if (existingChatId != default)
                {
                    return existingChatId;
                }
            }
        }

        Dictionary<Guid, (string FirstName, string LastName)> names =
            await projectMemberService.GetUserNamesByIdsAsync(new[] { userA, userB }, cancellationToken);

        string nameA = names.TryGetValue(userA, out (string FirstName, string LastName) nA)
            ? $"{nA.FirstName} {nA.LastName}".Trim()
            : userA.ToString();

        string nameB = names.TryGetValue(userB, out (string FirstName, string LastName) nB)
            ? $"{nB.FirstName} {nB.LastName}".Trim()
            : userB.ToString();

        ChatModel direct = ChatModel.CreateDirect(userA, userB, $"{nameA}, {nameB}");

        await chatWriteRepo.Insert(direct);
        await chatWriteRepo.SaveChangesAsync(cancellationToken);

        await chatMemberRepo.Insert(new ChatMember(direct.Id, userA, isAdmin: false));
        await chatMemberRepo.Insert(new ChatMember(direct.Id, userB, isAdmin: false));

        logger.LogInformation(
            "Auto-created direct chat {ChatId} between {UserA} and {UserB} after group shrink",
            direct.Id, userA, userB);

        Guid notifyUserId = userA == requestingUserId ? userB : userA;

        DateTime joinedAt = DateTime.UtcNow;
        List<ChatMemberWeb> memberWebs = new List<ChatMemberWeb>
        {
            new(userA, string.Empty, string.Empty, joinedAt, false, null),
            new(userB, string.Empty, string.Empty, joinedAt, false, null)
        };

        ChatWeb chatWeb = ChatMapper.MapChat(direct, memberWebs, lastMessage: null, unreadCount: 0);

        await hubContext.Clients
            .Group(ChatHubGroups.User(notifyUserId))
            .ChatCreated(chatWeb);

        return direct.Id;
    }
}
