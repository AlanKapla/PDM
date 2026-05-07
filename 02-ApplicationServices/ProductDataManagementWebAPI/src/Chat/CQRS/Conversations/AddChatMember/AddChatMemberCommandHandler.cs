using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Chat.DTOs;
using Chat.Hubs;
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
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.AddChatMember;

public sealed class AddChatMemberCommandHandler : IRequestHandler<AddChatMemberCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<AddChatMemberCommandHandler> logger;

    public AddChatMemberCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatModel> chatWriteRepo,
        IRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<AddChatMemberCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(AddChatMemberCommand request, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(c => c.Id == request.ChatId, cancellationToken);

        if (chat == null)
        {
            throw new NotFoundApiException("Chat", request.ChatId.ToString());
        }

        ChatMember? requesterMembership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id);

        if (requesterMembership == null)
        {
            throw new NotFoundApiException("ChatMember", currentUser.Id.ToString());
        }

        // Group chats require admin; direct chats (converting to group) allow any member
        if (chat.IsGroupChat && !requesterMembership.IsAdmin)
        {
            throw new ForbiddenApiException("Only chat admins can add members.");
        }

        bool alreadyMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == request.ChatId && cm.UserId == request.UserId, cancellationToken);

        if (alreadyMember)
        {
            throw new ConflictApiException("ChatMember", request.UserId.ToString());
        }

        // Use chat's existing project, or the one supplied when converting direct → group
        Guid? effectiveProjectId = chat.ProjectId ?? request.ProjectId;

        if (!effectiveProjectId.HasValue)
        {
            throw new ValidationApiException("ProjectId is required when adding a member to a direct chat.");
        }

        bool newMemberInProject = await projectMemberService.IsUserInProjectAsync(
            request.UserId, effectiveProjectId.Value, cancellationToken);

        if (!newMemberInProject)
        {
            throw new ForbiddenApiException("The user to be added must be a member of the chat's project.");
        }

        // Count existing members before insert — DB hasn't seen the new one yet
        int existingCount = await chatMemberRepo.CountAsync(
            cm => cm.ChatId == request.ChatId, cancellationToken);

        int newMemberCount = existingCount + 1;
        bool newIsGroupChat = newMemberCount > 2;

        ChatMember newMember = new ChatMember
        {
            ChatId = chat.Id,
            UserId = request.UserId,
            JoinedAt = DateTime.UtcNow,
            IsAdmin = false
        };

        await chatMemberRepo.Insert(newMember);

        if (chat.IsGroupChat != newIsGroupChat || chat.ProjectId != effectiveProjectId)
        {
            chat.IsGroupChat = newIsGroupChat;
            chat.ProjectId = effectiveProjectId;
            chat.TenantId = await projectMemberService.GetProjectTenantIdAsync(effectiveProjectId.Value, cancellationToken);
            await chatWriteRepo.Update(chat);
        }

        logger.LogInformation(
            "User {NewUserId} added to chat {ChatId} by {RequesterId} (now {MemberCount} members, IsGroupChat={IsGroupChat})",
            request.UserId, chat.Id, currentUser.Id, newMemberCount, newIsGroupChat);

        await NotifyAsync(chat, newMember, cancellationToken);

        return Unit.Value;
    }

    private async Task NotifyAsync(ChatModel chat, ChatMember newMember, CancellationToken cancellationToken)
    {
        ChatMemberWeb memberWeb = new ChatMemberWeb(
            newMember.UserId,
            string.Empty,
            string.Empty,
            newMember.JoinedAt,
            newMember.IsAdmin,
            null);

        await hubContext.Clients
            .Group($"chat:{chat.Id}")
            .MemberAdded(new MemberAddedPayload(chat.Id, memberWeb));

        ChatWeb chatWeb = new ChatWeb(
            Id: chat.Id,
            Name: chat.Name,
            IsGroupChat: chat.IsGroupChat,
            ProjectId: chat.ProjectId,
            TenantId: chat.TenantId,
            CreatedAt: chat.CreatedAt,
            CreatedByUserId: chat.CreatedByUserId,
            UnreadCount: 0,
            LastMessage: null,
            Members: new List<ChatMemberWeb> { memberWeb });

        await hubContext.Clients
            .Group($"user:{newMember.UserId}")
            .ChatCreated(chatWeb);
    }
}
