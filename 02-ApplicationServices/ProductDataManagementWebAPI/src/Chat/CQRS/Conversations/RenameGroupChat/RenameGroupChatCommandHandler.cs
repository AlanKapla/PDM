using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
        ChatModel? chat = await chatRepo.GetById(request.ChatId);

        if (chat == null)
        {
            throw new NotFoundApiException("Chat", request.ChatId.ToString());
        }

        if (!chat.IsGroupChat)
        {
            throw new ValidationApiException("Cannot rename a direct chat.");
        }

        ChatMember? membership = await chatMemberRepo.GetFirstBySearch(
            cm => cm.ChatId == request.ChatId &&
                  cm.UserId == currentUser.Id,
            cancellationToken);

        if (membership == null)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        if (!membership.IsAdmin)
        {
            throw new ForbiddenApiException("Only admins can rename a group chat.");
        }

        chat.Name = request.NewName;
        await chatWriteRepo.Update(chat);

        logger.LogInformation(
            "Group chat {ChatId} renamed to '{NewName}' by user {UserId}",
            chat.Id,
            request.NewName,
            currentUser.Id);

        return Unit.Value;
    }
}
