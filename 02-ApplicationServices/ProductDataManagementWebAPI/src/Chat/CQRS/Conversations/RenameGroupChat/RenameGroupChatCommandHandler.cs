using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.RenameGroupChat;

public sealed class RenameGroupChatCommandHandler : IRequestHandler<RenameGroupChatCommand, Unit>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatModel> chatWriteRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<RenameGroupChatCommandHandler> logger;

    public RenameGroupChatCommandHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatModel> chatWriteRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        ICurrentUser currentUser,
        ILogger<RenameGroupChatCommandHandler> logger)
    {
        this.chatRepo = chatRepo;
        this.chatWriteRepo = chatWriteRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<Unit> Handle(RenameGroupChatCommand request, CancellationToken cancellationToken)
    {
        ChatModel chat = await GetAndValidateChatAsync(request.TenantId, request.ChatId, cancellationToken);
        ChatMember membership = await GetAndValidateMembershipAsync(request.ChatId, currentUser.Id, cancellationToken);

        if (!membership.IsAdmin)
        {
            throw new ForbiddenApiException("Only admins can rename a group chat.");
        }

        chat.Rename(request.NewName);
        await chatWriteRepo.Update(chat);

        logger.LogInformation(
            "Group chat {ChatId} renamed to '{NewName}' by user {UserId}",
            chat.Id,
            request.NewName,
            currentUser.Id);

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
            cm => cm.ChatId == chatId && cm.UserId == userId,
            cancellationToken);

        if (membership is null)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        return membership;
    }
}
