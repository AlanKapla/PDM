using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Chat.Hubs;
using CQRS.PostCommit;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.RemoveChatMember;

public sealed class RemoveChatMemberCommandHandler : IRequestHandler<RemoveChatMemberCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly IPostCommitDispatcher dispatcher;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<RemoveChatMemberCommandHandler> logger;

    public RemoveChatMemberCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        IPostCommitDispatcher dispatcher,
        ICurrentUser currentUser,
        ILogger<RemoveChatMemberCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.dispatcher = dispatcher;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(RemoveChatMemberCommand request, CancellationToken cancellationToken)
    {
        ChatModel chat = await GetAndValidateChatAsync(request.TenantId, request.ChatId, cancellationToken);

        if (!chat.IsGroupChat)
        {
            throw new ValidationApiException("Members can only be removed from group chats.");
        }

        ChatMember requesterMembership = await GetAndValidateMembershipAsync(
            request.ChatId, currentUser.Id, cancellationToken);
        ChatMember targetMembership = await GetAndValidateMembershipAsync(
            request.ChatId, request.UserId, cancellationToken);

        EnsureRemovalAllowed(requesterMembership, targetMembership, request.UserId);

        await chatMemberRepo.Delete(targetMembership);

        logger.LogInformation(
            "User {RemovedUserId} removed from chat {ChatId} by {RequesterId}",
            request.UserId, chat.Id, currentUser.Id);

        Guid removedUserId = request.UserId;
        Guid chatIdForBroadcast = chat.Id;
        dispatcher.Enqueue(_ =>
            hubContext.Clients
                .Group(ChatHubGroups.User(removedUserId))
                .RemovedFromChat(new RemovedFromChatPayload(chatIdForBroadcast, null)));

        return Unit.Value;
    }

    private async Task<ChatModel> GetAndValidateChatAsync(Guid tenantId, Guid chatId, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(
            c => c.Id == chatId && c.TenantId == tenantId,
            cancellationToken);

        if (chat is null)
        {
            throw new NotFoundApiException(nameof(Entities.Models.Chats.Chat), chatId.ToString());
        }

        return chat;
    }

    private async Task<ChatMember> GetAndValidateMembershipAsync(
        Guid chatId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        ChatMember? membership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == chatId && cm.UserId == userId);

        if (membership is null)
        {
            throw new NotFoundApiException(nameof(ChatMember), userId.ToString());
        }

        return membership;
    }

    private void EnsureRemovalAllowed(
        ChatMember requesterMembership,
        ChatMember targetMembership,
        Guid targetUserId)
    {
        bool isSelfRemoval = currentUser.Id == targetUserId;

        if (isSelfRemoval)
        {
            return;
        }

        if (!requesterMembership.IsAdmin)
        {
            throw new ForbiddenApiException("Only chat admins can remove other members.");
        }

        if (targetMembership.IsAdmin)
        {
            throw new ForbiddenApiException("An admin cannot be removed by another admin. The member must leave voluntarily.");
        }
    }
}
