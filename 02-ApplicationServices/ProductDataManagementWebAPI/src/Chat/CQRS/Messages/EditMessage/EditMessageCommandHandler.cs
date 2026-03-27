using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using Entities.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Messages.EditMessage;

public sealed class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, Unit>
{
    private readonly IReadRepository<MessageHistory> messageRepo;
    private readonly IRepository<MessageHistory> messageWriteRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ChatOptions options;
    private readonly ILogger<EditMessageCommandHandler> logger;

    public EditMessageCommandHandler(
        IReadRepository<MessageHistory> messageRepo,
        IRepository<MessageHistory> messageWriteRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        IOptions<ChatOptions> options,
        ILogger<EditMessageCommandHandler> logger)
    {
        this.messageRepo = messageRepo;
        this.messageWriteRepo = messageWriteRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<Unit> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        MessageHistory? message = await messageRepo.GetFirstBySearch(
            m => m.Id == request.MessageId &&
                 m.ChatId == request.ChatId &&
                 m.UserId == currentUser.Id &&
                 m.DeletedAt == null,
            cancellationToken);

        if (message == null)
        {
            throw new NotFoundApiException("Message", request.MessageId.ToString());
        }

        if (DateTime.UtcNow - message.CreatedAt > options.MaxEditWindow)
        {
            throw new ValidationApiException(
                $"Messages can only be edited within {options.MaxMessageEditWindowMinutes} minutes of sending.");
        }

        message.Content = request.NewContent;
        message.EditedAt = DateTime.UtcNow;

        await messageWriteRepo.Update(message);
        await messageWriteRepo.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group($"chat:{request.ChatId}")
            .MessageEdited(new MessageEditedPayload(
                message.Id,
                request.ChatId,
                message.Content,
                message.EditedAt.Value));

        logger.LogDebug(
            "Message {MessageId} edited by user {UserId} in chat {ChatId}",
            message.Id,
            currentUser.Id,
            request.ChatId);

        return Unit.Value;
    }
}
