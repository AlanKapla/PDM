using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using CQRS.PostCommit;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.LeaveChat;

public sealed class LeaveChatCommandHandler : IRequestHandler<LeaveChatCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<LeaveChatCommandHandler> logger;

    public LeaveChatCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatModel> chatWriteRepo,
        IRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<LeaveChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(LeaveChatCommand request, CancellationToken cancellationToken)
    {
        ChatModel chat = await GetAndValidateChatAsync(request.ChatId, cancellationToken);
        ChatMember membership = await GetAndValidateMembershipAsync(request.ChatId, currentUser.Id, cancellationToken);

        if (membership.IsAdmin)
        {
            await DissolveGroupAsync(chat, cancellationToken);
        }
        else
        {
            await LeaveGroupAsync(chat, membership, cancellationToken);
        }

        return Unit.Value;
    }

    private async Task<ChatModel> GetAndValidateChatAsync(Guid chatId, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(c => c.Id == chatId, cancellationToken);

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

    private async Task DissolveGroupAsync(ChatModel chat, CancellationToken cancellationToken)
    {
        List<Guid> memberIds = await chatMemberRepo.SelectAsync(
            cm => cm.ChatId == chat.Id,
            cm => cm.UserId,
            cancellationToken);

        // DB cascade deletes ChatMembers and MessageHistories
        await chatWriteRepo.ExecuteDeleteAsync(c => c.Id == chat.Id, cancellationToken);

        Guid chatIdForBroadcast = chat.Id;
        foreach (Guid userId in memberIds)
        {
            Guid capturedUserId = userId;
            dispatcher.Enqueue(_ =>
                hubContext.Clients
                    .Group(ChatHubGroups.User(capturedUserId))
                    .ChatDeleted(chatIdForBroadcast));
        }

        logger.LogInformation(
            "Group chat {ChatId} dissolved by admin {UserId}",
            chat.Id, currentUser.Id);
    }

    private async Task LeaveGroupAsync(ChatModel chat, ChatMember membership, CancellationToken cancellationToken)
    {
        await chatMemberRepo.Delete(membership);

        Guid chatIdForBroadcast = chat.Id;
        Guid removedUserId = currentUser.Id;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.User(removedUserId))
                .RemovedFromChat(new RemovedFromChatPayload(chatIdForBroadcast, null)));

        logger.LogInformation(
            "User {UserId} left chat {ChatId}",
            removedUserId, chatIdForBroadcast);
    }
}
