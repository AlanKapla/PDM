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

namespace Chat.CQRS.Conversations.CreateGroupChat;

public sealed class CreateGroupChatCommandHandler : IRequestHandler<CreateGroupChatCommand, CreateChatResultWeb>
{
    private readonly IRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<CreateGroupChatCommandHandler> logger;

    public CreateGroupChatCommandHandler(
        IRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<CreateGroupChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<CreateChatResultWeb> Handle(CreateGroupChatCommand request, CancellationToken cancellationToken)
    {
        // Project-bound group chat requires ProjectId; tenant-only group chats are not supported yet.
        if (!request.ProjectId.HasValue)
        {
            throw new ValidationApiException("ProjectId is required for group chats.");
        }

        Guid projectId = request.ProjectId.Value;

        List<Guid> allMemberIds = request.MemberUserIds
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
        if (request.Name is not null)
        {
            chatName = request.Name;
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

        Guid? projectTenantId = await projectMemberService.GetProjectTenantIdAsync(projectId, cancellationToken);

        ChatModel chat = ChatModel.CreateGroup(
            name: chatName,
            tenantId: projectTenantId,
            projectId: projectId,
            createdByUserId: currentUser.Id);

        await chatRepo.Insert(chat);
        await chatRepo.SaveChangesAsync(cancellationToken);

        foreach (Guid userId in allMemberIds)
        {
            await chatMemberRepo.Insert(new ChatMember(
                chatId: chat.Id,
                userId: userId,
                isAdmin: userId == currentUser.Id));
        }

        logger.LogInformation(
            "Group chat {ChatId} '{Name}' created in project {ProjectId} by user {UserId}",
            chat.Id, chat.Name, projectId, currentUser.Id);

        ChatWeb groupChatWeb = BuildChatWeb(chat, allMemberIds);
        foreach (Guid userId in request.MemberUserIds)
        {
            Guid capturedUserId = userId;
            dispatcher.Enqueue(_ =>
                hubContext.Clients
                    .Group(ChatHubGroups.User(capturedUserId))
                    .ChatCreated(groupChatWeb));
        }

        return new CreateChatResultWeb(chat.Id, true);
    }

    private static ChatWeb BuildChatWeb(ChatModel chat, IEnumerable<Guid> memberIds)
    {
        DateTime now = DateTime.UtcNow;
        List<ChatMemberWeb> members = memberIds
            .Select(id => new ChatMemberWeb(id, string.Empty, string.Empty, now, false, null))
            .ToList();

        return ChatMapper.MapChat(chat, members, lastMessage: null, unreadCount: 0);
    }
}
