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

namespace Chat.CQRS.Conversations.DeleteChat;

public sealed class DeleteChatCommandHandler : IRequestHandler<DeleteChatCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<DeleteChatCommandHandler> logger;

    public DeleteChatCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatModel> chatWriteRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<DeleteChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(DeleteChatCommand request, CancellationToken cancellationToken)
    {
        ChatModel chat = await GetAndValidateChatAsync(request.TenantId, request.ChatId, cancellationToken);
        ChatMember membership = await GetAndValidateMembershipAsync(request.ChatId, currentUser.Id, cancellationToken);

        if (chat.IsGroupChat && !membership.IsAdmin)
        {
            throw new ForbiddenApiException("Only admins can delete a group chat.");
        }

        List<Guid> memberIds = await chatMemberRepo.SelectAsync(
            cm => cm.ChatId == request.ChatId,
            cm => cm.UserId,
            cancellationToken);

        // DB cascade deletes ChatMembers and MessageHistories
        await chatWriteRepo.ExecuteDeleteAsync(c => c.Id == request.ChatId, cancellationToken);

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
            "Chat {ChatId} deleted by user {UserId}",
            request.ChatId, currentUser.Id);

        return Unit.Value;
    }

    private async Task<ChatModel> GetAndValidateChatAsync(Guid tenantId, Guid chatId, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(
            c => c.Id == chatId && c.TenantId == tenantId,
            cancellationToken);

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
            cm => cm.ChatId == chatId && cm.UserId == userId,
            cancellationToken);

        if (membership is null)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        return membership;
    }
}
