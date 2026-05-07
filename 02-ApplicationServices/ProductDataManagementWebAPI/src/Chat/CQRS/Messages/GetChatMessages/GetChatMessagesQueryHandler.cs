using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Chat.DTOs;
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
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Messages.GetChatMessages;

public sealed class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, List<MessageWeb>>
{
    private readonly IReadRepository<MessageHistory> messageRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public GetChatMessagesQueryHandler(
        IReadRepository<MessageHistory> messageRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        ICurrentUser currentUser)
    {
        this.messageRepo = messageRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.currentUser = currentUser;
    }

    public async Task<List<MessageWeb>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        bool isMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id,
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        IEnumerable<MessageHistory> allMessages = await messageRepo.GetBySearch(
            m => m.ChatId == request.ChatId);

        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        List<MessageHistory> ordered = allMessages
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

        if (request.Before.HasValue)
        {
            int cursorIndex = ordered.FindIndex(m => m.Id == request.Before.Value);
            if (cursorIndex >= 0)
            {
                ordered = ordered.Skip(cursorIndex + 1).ToList();
            }
        }

        List<MessageHistory> page = ordered.Take(pageSize).ToList();

        HashSet<Guid> senderIds = page.Select(m => m.UserId).ToHashSet();

        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(senderIds, cancellationToken);

        return page
            .Select(m =>
            {
                userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) sender);
                return new MessageWeb(
                    Id: m.Id,
                    ChatId: m.ChatId,
                    SenderId: m.UserId,
                    SenderFirstName: sender.FirstName ?? string.Empty,
                    SenderLastName: sender.LastName ?? string.Empty,
                    Content: m.IsDeleted ? string.Empty : m.Content,
                    IsDeleted: m.IsDeleted,
                    IsEdited: m.EditedAt.HasValue,
                    SentAt: m.CreatedAt,
                    EditedAt: m.EditedAt,
                    ReplyToMessageId: m.ReplyToMessageId);
            })
            .ToList();
    }
}
