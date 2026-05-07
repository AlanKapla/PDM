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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.CreateChat;

public sealed class CreateChatCommandHandler : IRequestHandler<CreateChatCommand, CreateChatResultWeb>
{
    private readonly IRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<CreateChatCommandHandler> logger;

    public CreateChatCommandHandler(
        IRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<CreateChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<CreateChatResultWeb> Handle(CreateChatCommand request, CancellationToken cancellationToken)
    {
        bool isDirect = request.MemberUserIds.Count == 1;

        return isDirect
            ? await HandleDirectAsync(request.MemberUserIds[0], request.Name, cancellationToken)
            : await HandleGroupAsync(request.ProjectId!.Value, request.MemberUserIds, request.Name, cancellationToken);
    }

    private async Task<CreateChatResultWeb> HandleDirectAsync(
        Guid targetUserId,
        string? requestedName,
        CancellationToken cancellationToken)
    {
        ProjectMember? sharedProject = await projectMemberService.FindSharedProjectAsync(
            currentUser.Id, targetUserId, cancellationToken);

        if (sharedProject == null)
        {
            throw new ForbiddenApiException("A direct chat can only be created between users who share at least one project.");
        }

        // Idempotency: return existing direct chat if one already exists between the two users
        IEnumerable<ChatMember> myMemberships = await chatMemberRepo.GetBySearch(
            cm => cm.UserId == currentUser.Id,
            include => include.Include(cm => cm.Chat));

        foreach (ChatMember membership in myMemberships.Where(cm => !cm.Chat.IsGroupChat))
        {
            bool targetAlsoMember = await chatMemberRepo.AnyAsync(
                cm => cm.ChatId == membership.ChatId && cm.UserId == targetUserId,
                cancellationToken);

            if (targetAlsoMember)
            {
                logger.LogDebug(
                    "Direct chat between {UserA} and {UserB} already exists: {ChatId}",
                    currentUser.Id, targetUserId, membership.ChatId);

                return new CreateChatResultWeb(membership.ChatId, false);
            }
        }

        string initiatorName = currentUser.FullName;
        string targetName = await projectMemberService.GetUserDisplayNameAsync(targetUserId, cancellationToken);

        ChatModel chat = new ChatModel
        {
            Name = $"{initiatorName}, {targetName}",
            IsGroupChat = false,
            ProjectId = null,
            TenantId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUser.Id
        };

        await chatRepo.Insert(chat);
        await chatRepo.SaveChangesAsync(cancellationToken);

        await chatMemberRepo.Insert(new ChatMember { ChatId = chat.Id, UserId = currentUser.Id, JoinedAt = DateTime.UtcNow, IsAdmin = false });
        await chatMemberRepo.Insert(new ChatMember { ChatId = chat.Id, UserId = targetUserId, JoinedAt = DateTime.UtcNow, IsAdmin = false });

        logger.LogInformation(
            "Direct chat {ChatId} created between users {UserA} and {UserB}",
            chat.Id, currentUser.Id, targetUserId);

        await hubContext.Clients
            .Group($"user:{targetUserId}")
            .ChatCreated(BuildChatWeb(chat, new List<Guid> { currentUser.Id, targetUserId }));

        return new CreateChatResultWeb(chat.Id, false);
    }

    private async Task<CreateChatResultWeb> HandleGroupAsync(
        Guid projectId,
        List<Guid> otherMemberIds,
        string? requestedName,
        CancellationToken cancellationToken)
    {
        List<Guid> allMemberIds = otherMemberIds
            .Append(currentUser.Id)
            .Distinct()
            .ToList();

        bool allInProject = await projectMemberService.AreAllMembersOfProjectAsync(
            projectId, allMemberIds, cancellationToken);

        if (!allInProject)
        {
            throw new ForbiddenApiException("All group chat members must be members of the specified project.");
        }

        string chatName;
        if (requestedName != null)
        {
            chatName = requestedName;
        }
        else
        {
            Dictionary<Guid, (string FirstName, string LastName)> names =
                await projectMemberService.GetUserNamesByIdsAsync(allMemberIds, cancellationToken);

            chatName = string.Join(", ", allMemberIds
                .Select(id => names.TryGetValue(id, out (string FirstName, string LastName) n)
                    ? $"{n.FirstName} {n.LastName}".Trim()
                    : id.ToString()));
        }

        ChatModel chat = new ChatModel
        {
            Name = chatName,
            IsGroupChat = true,
            ProjectId = projectId,
            TenantId = await projectMemberService.GetProjectTenantIdAsync(projectId, cancellationToken),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUser.Id
        };

        await chatRepo.Insert(chat);
        await chatRepo.SaveChangesAsync(cancellationToken);

        foreach (Guid userId in allMemberIds)
        {
            await chatMemberRepo.Insert(new ChatMember
            {
                ChatId = chat.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                IsAdmin = userId == currentUser.Id
            });
        }

        logger.LogInformation(
            "Group chat {ChatId} '{Name}' created in project {ProjectId} by user {UserId}",
            chat.Id, chat.Name, projectId, currentUser.Id);

        foreach (Guid userId in otherMemberIds)
        {
            await hubContext.Clients
                .Group($"user:{userId}")
                .ChatCreated(BuildChatWeb(chat, allMemberIds));
        }

        return new CreateChatResultWeb(chat.Id, true);
    }

    private static ChatWeb BuildChatWeb(ChatModel chat, IEnumerable<Guid> memberIds)
    {
        List<ChatMemberWeb> members = memberIds
            .Select(id => new ChatMemberWeb(id, string.Empty, string.Empty, DateTime.UtcNow, false, null))
            .ToList();

        return new ChatWeb(
            Id: chat.Id,
            Name: chat.Name,
            IsGroupChat: chat.IsGroupChat,
            ProjectId: chat.ProjectId,
            TenantId: chat.TenantId,
            CreatedAt: chat.CreatedAt,
            CreatedByUserId: chat.CreatedByUserId,
            UnreadCount: 0,
            LastMessage: null,
            Members: members);
    }
}
