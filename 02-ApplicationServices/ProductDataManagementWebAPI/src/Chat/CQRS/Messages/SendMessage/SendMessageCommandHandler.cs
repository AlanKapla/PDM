using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Chats;
using Chat.Hubs;
using Chat.Mappers;
using CQRS.PostCommit;
using Entities.Models.Chats;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Messages.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IRepository<MessageHistory> messageRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<SendMessageCommandHandler> logger;

    public SendMessageCommandHandler(
        IRepository<MessageHistory> messageRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<SendMessageCommandHandler> logger)
    {
        this.messageRepo = messageRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        await EnsureMembershipAsync(request.ChatId, cancellationToken);
        await EnsureReplyTargetExistsAsync(request.ChatId, request.ReplyToMessageId, cancellationToken);

        MessageHistory message = MessageHistory.Create(
            chatId: request.ChatId,
            authorId: currentUser.Id,
            content: request.Content,
            replyToId: request.ReplyToMessageId);

        await messageRepo.Insert(message);

        // Explicit save before broadcast: message must be persisted before
        // SignalR clients can query it via HTTP after receiving the real-time event.
        await messageRepo.SaveChangesAsync(cancellationToken);

        MessageWeb messageWeb = ChatMapper.MapMessage(message, currentUser.FirstName, currentUser.LastName);

        // Broadcast deferred until after transaction commit (TransactionBehavior + IPostCommitDispatcher).
        Guid chatIdForBroadcast = request.ChatId;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.Chat(chatIdForBroadcast))
                .ReceiveMessage(messageWeb));

        logger.LogDebug(
            "Message {MessageId} sent to chat {ChatId} by user {UserId}",
            message.Id,
            request.ChatId,
            currentUser.Id);

        return message.Id;
    }

    private async Task EnsureMembershipAsync(Guid chatId, CancellationToken cancellationToken)
    {
        bool isMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == chatId && cm.UserId == currentUser.Id,
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }
    }

    private async Task EnsureReplyTargetExistsAsync(
        Guid chatId,
        Guid? replyToMessageId,
        CancellationToken cancellationToken)
    {
        if (replyToMessageId is null)
        {
            return;
        }

        bool replyExists = await messageRepo.AnyAsync(
            m => m.Id == replyToMessageId.Value &&
                 m.ChatId == chatId &&
                 m.DeletedAt == null,
            cancellationToken);

        if (!replyExists)
        {
            throw new NotFoundApiException(nameof(MessageHistory), replyToMessageId.Value.ToString());
        }
    }
}
