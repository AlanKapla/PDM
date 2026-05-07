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
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.LeaveChat;

public sealed class LeaveChatCommandHandler : IRequestHandler<LeaveChatCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<LeaveChatCommandHandler> logger;

    public LeaveChatCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatModel> chatWriteRepo,
        IRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<LeaveChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(LeaveChatCommand request, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(c => c.Id == request.ChatId, cancellationToken);

        if (chat == null)
        {
            throw new NotFoundApiException("Chat", request.ChatId.ToString());
        }

        ChatMember? membership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id);

        if (membership == null)
        {
            throw new NotFoundApiException("ChatMember", currentUser.Id.ToString());
        }

        if (membership.IsAdmin)
        {
            await DissolveGroupAsync(chat, cancellationToken);
        }
        else
        {
            await LeaveGroupAsync(chat, membership, cancellationToken);
        }

        return Unit.Value;
    }

    private async Task DissolveGroupAsync(ChatModel chat, CancellationToken cancellationToken)
    {
        List<Guid> memberIds = await chatMemberRepo.SelectAsync(
            cm => cm.ChatId == chat.Id,
            cm => cm.UserId,
            cancellationToken);

        foreach (Guid userId in memberIds)
        {
            await hubContext.Clients
                .Group(ChatHubGroups.User(userId))
                .ChatDeleted(chat.Id);
        }

        // DB cascade deletes ChatMembers and MessageHistories
        await chatWriteRepo.ExecuteDeleteAsync(c => c.Id == chat.Id, cancellationToken);

        logger.LogInformation(
            "Group chat {ChatId} dissolved by admin {UserId}",
            chat.Id, currentUser.Id);
    }

    private async Task LeaveGroupAsync(ChatModel chat, ChatMember membership, CancellationToken cancellationToken)
    {
        await chatMemberRepo.Delete(membership);

        await hubContext.Clients
            .Group(ChatHubGroups.User(currentUser.Id))
            .RemovedFromChat(new RemovedFromChatPayload(chat.Id, null));

        logger.LogInformation(
            "User {UserId} left chat {ChatId}",
            currentUser.Id, chat.Id);
    }
}
