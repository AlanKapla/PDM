using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.DTOs;
using Chat.Hubs;
using Entities.Models;
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
    private readonly ICurrentUser currentUser;
    private readonly ILogger<SendMessageCommandHandler> logger;

    public SendMessageCommandHandler(
        IRepository<MessageHistory> messageRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<SendMessageCommandHandler> logger)
    {
        this.messageRepo = messageRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        List<Guid> memberIds = await chatMemberRepo.SelectAsync(
            cm => cm.ChatId == request.ChatId,
            cm => cm.UserId,
            cancellationToken);

        if (!memberIds.Contains(currentUser.Id))
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        MessageHistory message = new MessageHistory
        {
            ChatId = request.ChatId,
            UserId = currentUser.Id,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            ReplyToMessageId = request.ReplyToMessageId
        };

        await messageRepo.Insert(message);

        // Explicit save before broadcast: message must be persisted before
        // SignalR clients can query it via HTTP after receiving the real-time event.
        await messageRepo.SaveChangesAsync(cancellationToken);

        MessageWeb messageWeb = new MessageWeb(
            Id: message.Id,
            ChatId: message.ChatId,
            SenderId: message.UserId,
            SenderFirstName: currentUser.FirstName,
            SenderLastName: currentUser.LastName,
            Content: message.Content,
            IsDeleted: false,
            IsEdited: false,
            SentAt: message.CreatedAt,
            EditedAt: null,
            ReplyToMessageId: message.ReplyToMessageId);

        foreach (Guid recipientId in memberIds.Where(id => id != currentUser.Id))
        {
            await hubContext.Clients
                .Group($"user:{recipientId}")
                .ReceiveMessage(messageWeb);
        }

        logger.LogDebug(
            "Message {MessageId} sent to chat {ChatId} by user {UserId}",
            message.Id,
            request.ChatId,
            currentUser.Id);

        return message.Id;
    }
}
