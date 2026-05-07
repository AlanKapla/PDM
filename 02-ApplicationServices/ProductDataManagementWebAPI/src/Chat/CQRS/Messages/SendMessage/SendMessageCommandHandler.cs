using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.DTOs;
using Chat.Hubs;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
        bool isMember = await chatMemberRepo.AnyAsync(
             cm => cm.ChatId == request.ChatId &&
                   cm.UserId == currentUser.Id,
             cancellationToken);

        if (!isMember)
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

        await hubContext.Clients
            .Group(ChatHubGroups.Chat(request.ChatId))
            .ReceiveMessage(messageWeb);

        logger.LogDebug(
            "Message {MessageId} sent to chat {ChatId} by user {UserId}",
            message.Id,
            request.ChatId,
            currentUser.Id);

        return message.Id;
    }
}
