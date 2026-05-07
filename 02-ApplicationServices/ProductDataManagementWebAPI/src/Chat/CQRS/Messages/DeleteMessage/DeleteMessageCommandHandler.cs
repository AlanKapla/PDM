using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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

namespace Chat.CQRS.Messages.DeleteMessage;

public sealed class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, Unit>
{
    private readonly IReadRepository<MessageHistory> messageRepo;
    private readonly IRepository<MessageHistory> messageWriteRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<DeleteMessageCommandHandler> logger;

    public DeleteMessageCommandHandler(
        IReadRepository<MessageHistory> messageRepo,
        IRepository<MessageHistory> messageWriteRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<DeleteMessageCommandHandler> logger)
    {
        this.messageRepo = messageRepo;
        this.messageWriteRepo = messageWriteRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        MessageHistory? message = await messageRepo.GetFirstBySearch(
            m => m.Id == request.MessageId &&
                 m.ChatId == request.ChatId &&
                 m.UserId == currentUser.Id &&
                 m.DeletedAt == null,
            cancellationToken)
            ?? throw new NotFoundApiException("Message", request.MessageId.ToString());

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        await messageWriteRepo.Update(message);
        await messageWriteRepo.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(ChatHubGroups.Chat(request.ChatId))
            .MessageDeleted(new MessageDeletedPayload(message.Id, request.ChatId));

        logger.LogDebug(
            "Message {MessageId} soft-deleted by user {UserId} in chat {ChatId}",
            message.Id,
            currentUser.Id,
            request.ChatId);

        return Unit.Value;
    }
}
