using Business.Interfaces.Services;
using Chat.DTOs;
using Chat.Hubs;
using Entities.Models;
using ChatModel = Entities.Models.Chat;
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

        ChatModel direct = new ChatModel
        {
            Name = $"{nameA}, {nameB}",
            IsGroupChat = false,
            ProjectId = null,
            TenantId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userA
        };

        await chatWriteRepo.Insert(direct);
        await chatWriteRepo.SaveChangesAsync(cancellationToken);

        await chatMemberRepo.Insert(new ChatMember { ChatId = direct.Id, UserId = userA, JoinedAt = DateTime.UtcNow, IsAdmin = false });
        await chatMemberRepo.Insert(new ChatMember { ChatId = direct.Id, UserId = userB, JoinedAt = DateTime.UtcNow, IsAdmin = false });

        logger.LogInformation(
            "Auto-created direct chat {ChatId} between {UserA} and {UserB} after group shrink",
            direct.Id, userA, userB);

        Guid notifyUserId = userA == requestingUserId ? userB : userA;

        ChatWeb chatWeb = new ChatWeb(
            Id: direct.Id,
            Name: direct.Name,
            IsGroupChat: false,
            ProjectId: null,
            TenantId: null,
            CreatedAt: direct.CreatedAt,
            CreatedByUserId: direct.CreatedByUserId,
            UnreadCount: 0,
            LastMessage: null,
            Members: new List<ChatMemberWeb>
            {
                new(userA, string.Empty, string.Empty, DateTime.UtcNow, false, null),
                new(userB, string.Empty, string.Empty, DateTime.UtcNow, false, null)
            });

        await hubContext.Clients
            .Group($"user:{notifyUserId}")
            .ChatCreated(chatWeb);

        return direct.Id;
    }
}
