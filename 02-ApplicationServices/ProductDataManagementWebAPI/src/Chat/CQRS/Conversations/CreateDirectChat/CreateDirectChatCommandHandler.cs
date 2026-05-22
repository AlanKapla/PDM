using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using Chat.Hubs;
using Chat.Mappers;
using CQRS.PostCommit;
using Entities.Models.Chats;
using Entities.Models.Projects;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.CreateDirectChat;

public sealed class CreateDirectChatCommandHandler : IRequestHandler<CreateDirectChatCommand, CreateChatResultWeb>
{
    private readonly IRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<CreateDirectChatCommandHandler> logger;

    public CreateDirectChatCommandHandler(
        IRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<CreateDirectChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<CreateChatResultWeb> Handle(CreateDirectChatCommand request, CancellationToken cancellationToken)
    {
        Guid targetUserId = request.TargetUserId;

        ProjectMember? sharedProject = await projectMemberService.FindSharedProjectAsync(
            currentUser.Id, targetUserId, cancellationToken);

        if (sharedProject is null)
        {
            throw new ForbiddenApiException("A direct chat can only be created between users who share at least one project.");
        }

        // Idempotency: return existing direct chat if one already exists between the two users.
        // Single SQL query: a direct chat with exactly two members {currentUser, target}.
        ChatModel? existing = await chatRepo.GetFirstBySearch(
            c => !c.IsGroupChat
                 && c.Members.Count == 2
                 && c.Members.Any(m => m.UserId == currentUser.Id)
                 && c.Members.Any(m => m.UserId == targetUserId));

        if (existing is not null)
        {
            logger.LogDebug(
                "Direct chat between {UserA} and {UserB} already exists: {ChatId}",
                currentUser.Id, targetUserId, existing.Id);

            return new CreateChatResultWeb(existing.Id, false);
        }

        string initiatorName = currentUser.FullName;
        string targetName = await projectMemberService.GetUserDisplayNameAsync(targetUserId, cancellationToken);

        ChatModel chat = ChatModel.CreateDirect(currentUser.Id, targetUserId, $"{initiatorName}, {targetName}");

        await chatRepo.Insert(chat);
        await chatRepo.SaveChangesAsync(cancellationToken);

        await chatMemberRepo.Insert(new ChatMember(chat.Id, currentUser.Id, isAdmin: false));
        await chatMemberRepo.Insert(new ChatMember(chat.Id, targetUserId, isAdmin: false));

        logger.LogInformation(
            "Direct chat {ChatId} created between users {UserA} and {UserB}",
            chat.Id, currentUser.Id, targetUserId);

        ChatWeb directChatWeb = BuildChatWeb(chat, new List<Guid> { currentUser.Id, targetUserId });
        Guid directTargetUserId = targetUserId;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.User(directTargetUserId))
                .ChatCreated(directChatWeb));

        return new CreateChatResultWeb(chat.Id, false);
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
