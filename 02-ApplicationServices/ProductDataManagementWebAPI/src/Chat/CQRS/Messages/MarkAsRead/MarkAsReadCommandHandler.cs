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

namespace Chat.CQRS.Messages.MarkAsRead;

public sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Unit>
{
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<MarkAsReadCommandHandler> logger;

    public MarkAsReadCommandHandler(
        IRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<MarkAsReadCommandHandler> logger)
    {
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        ChatMember? membership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId &&
                  cm.UserId == currentUser.Id);

        if (membership == null)
        {
            throw new NotFoundApiException("ChatMember", currentUser.Id.ToString());
        }

        DateTime readAt = DateTime.UtcNow;
        membership.LastReadAt = readAt;
        await chatMemberRepo.Update(membership);
        await chatMemberRepo.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(ChatHubGroups.Chat(request.ChatId))
            .ReadReceipt(new ReadReceiptPayload(request.ChatId, currentUser.Id, readAt));

        logger.LogDebug(
            "User {UserId} marked chat {ChatId} as read",
            currentUser.Id,
            request.ChatId);

        return Unit.Value;
    }
}
