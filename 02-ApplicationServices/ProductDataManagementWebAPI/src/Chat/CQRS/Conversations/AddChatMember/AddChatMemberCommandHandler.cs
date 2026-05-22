using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using Chat.Hubs;
using Chat.Mappers;
using CQRS.PostCommit;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.AddChatMember;

public sealed class AddChatMemberCommandHandler : IRequestHandler<AddChatMemberCommand, Unit>
{
    private readonly IRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<AddChatMemberCommandHandler> logger;

    public AddChatMemberCommandHandler(
        IRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<AddChatMemberCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(AddChatMemberCommand request, CancellationToken cancellationToken)
    {
        ChatModel chat = await GetAndValidateChatAsync(request.TenantId, request.ChatId, cancellationToken);
        ChatMember requesterMembership = await GetAndValidateMembershipAsync(request.ChatId, currentUser.Id, cancellationToken);

        // Group chats require admin; direct chats (converting to group) allow any member
        if (chat.IsGroupChat && !requesterMembership.IsAdmin)
        {
            throw new ForbiddenApiException("Only chat admins can add members.");
        }

        await EnsureUserNotAlreadyMemberAsync(request.ChatId, request.UserId, cancellationToken);

        // Use chat's existing project, or the one supplied when converting direct → group
        Guid effectiveProjectId = ResolveEffectiveProjectId(chat, request.ProjectId);

        await EnsureNewMemberInProjectAsync(request.UserId, effectiveProjectId, cancellationToken);

        int newMemberCount = await chatMemberRepo.CountAsync(
            cm => cm.ChatId == request.ChatId, cancellationToken) + 1;
        bool newIsGroupChat = newMemberCount > 2;

        ChatMember newMember = new ChatMember(chat.Id, request.UserId, isAdmin: false);
        await chatMemberRepo.Insert(newMember);

        if (chat.IsGroupChat != newIsGroupChat || chat.ProjectId != effectiveProjectId)
        {
            Guid? newTenantId = await projectMemberService.GetProjectTenantIdAsync(effectiveProjectId, cancellationToken);
            chat.ConvertToGroup(effectiveProjectId, newTenantId);
            await chatRepo.Update(chat);
        }

        logger.LogInformation(
            "User {NewUserId} added to chat {ChatId} by {RequesterId} (now {MemberCount} members, IsGroupChat={IsGroupChat})",
            request.UserId, chat.Id, currentUser.Id, newMemberCount, newIsGroupChat);

        await NotifyAsync(chat, newMember, cancellationToken);

        return Unit.Value;
    }

    private async Task<ChatModel> GetAndValidateChatAsync(Guid tenantId, Guid chatId, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(
            c => c.Id == chatId && c.TenantId == tenantId);

        if (chat is null)
        {
            throw new NotFoundApiException(nameof(Entities.Models.Chats.Chat), chatId.ToString());
        }

        return chat;
    }

    private async Task<ChatMember> GetAndValidateMembershipAsync(
        Guid chatId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        ChatMember? membership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == chatId && cm.UserId == userId);

        if (membership is null)
        {
            throw new NotFoundApiException(nameof(ChatMember), userId.ToString());
        }

        return membership;
    }

    private async Task EnsureUserNotAlreadyMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        bool alreadyMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == chatId && cm.UserId == userId, cancellationToken);

        if (alreadyMember)
        {
            throw new ConflictApiException(nameof(ChatMember), userId.ToString());
        }
    }

    private static Guid ResolveEffectiveProjectId(ChatModel chat, Guid? requestedProjectId)
    {
        Guid? effectiveProjectId = chat.ProjectId ?? requestedProjectId;

        if (!effectiveProjectId.HasValue)
        {
            throw new ValidationApiException("ProjectId is required when adding a member to a direct chat.");
        }

        return effectiveProjectId.Value;
    }

    private async Task EnsureNewMemberInProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        bool inProject = await projectMemberService.IsUserInProjectAsync(userId, projectId, cancellationToken);

        if (!inProject)
        {
            throw new ForbiddenApiException("The user to be added must be a member of the chat's project.");
        }
    }

    private Task NotifyAsync(ChatModel chat, ChatMember newMember, CancellationToken cancellationToken)
    {
        ChatMemberWeb memberWeb = ChatMapper.MapMember(newMember, string.Empty, string.Empty);

        Guid chatId = chat.Id;
        Guid newUserId = newMember.UserId;

        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.Chat(chatId))
                .MemberAdded(new MemberAddedPayload(chatId, memberWeb)));

        ChatWeb chatWeb = ChatMapper.MapChat(
            chat,
            new List<ChatMemberWeb> { memberWeb },
            lastMessage: null,
            unreadCount: 0);

        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.User(newUserId))
                .ChatCreated(chatWeb));

        return Task.CompletedTask;
    }
}
