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

namespace Chat.CQRS.Conversations.RemoveChatMember;

public sealed class RemoveChatMemberCommandHandler : IRequestHandler<RemoveChatMemberCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IHubContext<ChatHub, IChatClient> hubContext;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<RemoveChatMemberCommandHandler> logger;

    public RemoveChatMemberCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
        IHubContext<ChatHub, IChatClient> hubContext,
        ICurrentUser currentUser,
        ILogger<RemoveChatMemberCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.hubContext = hubContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(RemoveChatMemberCommand request, CancellationToken cancellationToken)
    {
        ChatModel? chat = await chatRepo.GetFirstBySearch(c => c.Id == request.ChatId, cancellationToken);

        if (chat == null)
        {
            throw new NotFoundApiException("Chat", request.ChatId.ToString());
        }

        if (!chat.IsGroupChat)
        {
            throw new ValidationApiException("Members can only be removed from group chats.");
        }

        ChatMember? requesterMembership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id);

        if (requesterMembership == null)
        {
            throw new NotFoundApiException("ChatMember", currentUser.Id.ToString());
        }

        ChatMember? targetMembership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId && cm.UserId == request.UserId);

        if (targetMembership == null)
        {
            throw new NotFoundApiException("ChatMember", request.UserId.ToString());
        }

        bool isSelfRemoval = currentUser.Id == request.UserId;

        if (!isSelfRemoval)
        {
            if (!requesterMembership.IsAdmin)
            {
                throw new ForbiddenApiException("Only chat admins can remove other members.");
            }

            if (targetMembership.IsAdmin)
            {
                throw new ForbiddenApiException("An admin cannot be removed by another admin. The member must leave voluntarily.");
            }
        }

        await chatMemberRepo.Delete(targetMembership);

        logger.LogInformation(
            "User {RemovedUserId} removed from chat {ChatId} by {RequesterId}",
            request.UserId, chat.Id, currentUser.Id);

        await hubContext.Clients
            .Group(ChatHubGroups.User(request.UserId))
            .RemovedFromChat(new RemovedFromChatPayload(chat.Id, null));

        return Unit.Value;
    }
}
