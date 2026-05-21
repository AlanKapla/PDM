using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using CQRS.PostCommit;
using Entities.Models.Chats;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Messages.EditMessage;

public sealed class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, Unit>
{
    private readonly IRepository<MessageHistory> messageRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ChatOptions options;
    private readonly ILogger<EditMessageCommandHandler> logger;

    public EditMessageCommandHandler(
        IRepository<MessageHistory> messageRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        IOptions<ChatOptions> options,
        ILogger<EditMessageCommandHandler> logger)
    {
        this.messageRepo = messageRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<Unit> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        MessageHistory message = await GetAndValidateMessageAsync(request.ChatId, request.MessageId, cancellationToken);

        EnsureWithinEditWindow(message);

        DateTime editedAtUtc = DateTime.UtcNow;
        message.Edit(request.NewContent, editedAtUtc);

        await messageRepo.Update(message);
        await messageRepo.SaveChangesAsync(cancellationToken);

        Guid messageId = message.Id;
        Guid chatIdForBroadcast = request.ChatId;
        string newContent = message.Content;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.Chat(chatIdForBroadcast))
                .MessageEdited(new MessageEditedPayload(
                    messageId,
                    chatIdForBroadcast,
                    newContent,
                    editedAtUtc)));

        logger.LogDebug(
            "Message {MessageId} edited by user {UserId} in chat {ChatId}",
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

    private void EnsureWithinEditWindow(MessageHistory message)
    {
        if (DateTime.UtcNow - message.CreatedAt > options.MaxEditWindow)
        {
            throw new ValidationApiException(
                $"Messages can only be edited within {options.MaxMessageEditWindowMinutes} minutes of sending.");
        }
    }
}
