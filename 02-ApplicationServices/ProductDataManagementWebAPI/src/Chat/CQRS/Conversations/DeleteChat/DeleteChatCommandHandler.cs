using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using Entities.Models;
using ChatModel = Entities.Models.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.DeleteChat;

public sealed class DeleteChatCommandHandler : IRequestHandler<DeleteChatCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<DeleteChatCommandHandler> logger;

    public DeleteChatCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatModel> chatWriteRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<DeleteChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(DeleteChatCommand request, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(c => c.Id == request.ChatId, cancellationToken);

        if (chat == null)
        {
            throw new NotFoundApiException("Chat", request.ChatId.ToString());
        }

        ChatMember? membership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id,
            cancellationToken);

        if (membership == null)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        if (chat.IsGroupChat && !membership.IsAdmin)
        {
            throw new ForbiddenApiException("Only admins can delete a group chat.");
        }

        List<Guid> memberIds = await chatMemberRepo.SelectAsync(
            cm => cm.ChatId == request.ChatId,
            cm => cm.UserId,
            cancellationToken);

        foreach (Guid userId in memberIds)
        {
            await hubContext.Clients
                .Group(ChatHubGroups.User(userId))
                .ChatDeleted(chat.Id);
        }

        // DB cascade deletes ChatMembers and MessageHistories
        await chatWriteRepo.ExecuteDeleteAsync(c => c.Id == request.ChatId, cancellationToken);

        logger.LogInformation(
            "Chat {ChatId} deleted by user {UserId}",
            request.ChatId, currentUser.Id);

        return Unit.Value;
    }
}
