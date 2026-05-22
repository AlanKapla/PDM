using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using CQRS.PostCommit;
using Entities.Models.Chats;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Messages.DeleteMessage;

public sealed class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, Unit>
{
    private readonly IRepository<MessageHistory> messageRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<DeleteMessageCommandHandler> logger;

    public DeleteMessageCommandHandler(
        IRepository<MessageHistory> messageRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<DeleteMessageCommandHandler> logger)
    {
        this.messageRepo = messageRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        MessageHistory message = await GetAndValidateMessageAsync(request.ChatId, request.MessageId, cancellationToken);

        message.SoftDelete(DateTime.UtcNow);
        await messageRepo.Update(message);
        await messageRepo.SaveChangesAsync(cancellationToken);

        Guid messageId = message.Id;
        Guid chatIdForBroadcast = request.ChatId;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.Chat(chatIdForBroadcast))
                .MessageDeleted(new MessageDeletedPayload(messageId, chatIdForBroadcast)));

        logger.LogDebug(
            "Message {MessageId} soft-deleted by user {UserId} in chat {ChatId}",
            message.Id,
            currentUser.Id,
            request.ChatId);

        return Unit.Value;
    }

    private async Task<MessageHistory> GetAndValidateMessageAsync(
        Guid chatId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        MessageHistory? message = await messageRepo.GetFirstBySearch(
            m => m.Id == messageId &&
                 m.ChatId == chatId &&
                 m.UserId == currentUser.Id &&
                 m.DeletedAt == null);

        if (message is null)
        {
            throw new NotFoundApiException(nameof(MessageHistory), messageId.ToString());
        }

        return message;
    }
}
