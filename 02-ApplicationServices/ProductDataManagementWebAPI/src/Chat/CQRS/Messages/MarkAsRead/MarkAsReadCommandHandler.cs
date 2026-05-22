using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using CQRS.PostCommit;
using Entities.Models.Chats;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Messages.MarkAsRead;

public sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Unit>
{
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<MarkAsReadCommandHandler> logger;

    public MarkAsReadCommandHandler(
        IRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<MarkAsReadCommandHandler> logger)
    {
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        ChatMember membership = await GetAndValidateMembershipAsync(request.ChatId, currentUser.Id, cancellationToken);

        DateTime readAt = DateTime.UtcNow;
        membership.MarkRead(readAt);
        await chatMemberRepo.Update(membership);
        await chatMemberRepo.SaveChangesAsync(cancellationToken);

        Guid chatIdForBroadcast = request.ChatId;
        Guid readerUserId = currentUser.Id;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.Chat(chatIdForBroadcast))
                .ReadReceipt(new ReadReceiptPayload(chatIdForBroadcast, readerUserId, readAt)));

        logger.LogDebug(
            "User {UserId} marked chat {ChatId} as read",
            currentUser.Id,
            request.ChatId);

        return Unit.Value;
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
}
