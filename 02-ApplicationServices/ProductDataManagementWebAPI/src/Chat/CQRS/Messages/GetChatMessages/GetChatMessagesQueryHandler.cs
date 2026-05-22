using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using Chat.Mappers;
using Entities.Models.Chats;
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

        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        List<MessageHistory> page;
        if (request.Before is null)
        {
            page = await messageRepo.GetPagedBySearchAsync(
                m => m.ChatId == request.ChatId && m.DeletedAt == null,
                q => q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id),
                pageSize,
                cancellationToken);
        }
        else
        {
            MessageHistory? cursor = await messageRepo.GetFirstBySearch(
                m => m.Id == request.Before.Value,
                cancellationToken);

            if (cursor is null)
            {
                throw new NotFoundApiException(nameof(MessageHistory), request.Before.Value.ToString());
            }

            DateTime cursorCreatedAt = cursor.CreatedAt;
            Guid cursorId = cursor.Id;

            page = await messageRepo.GetPagedBySearchAsync(
                m => m.ChatId == request.ChatId
                     && m.DeletedAt == null
                     && (m.CreatedAt < cursorCreatedAt
                         || (m.CreatedAt == cursorCreatedAt && m.Id.CompareTo(cursorId) < 0)),
                q => q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id),
                pageSize,
                cancellationToken);
        }

        HashSet<Guid> senderIds = page.Select(m => m.UserId).ToHashSet();

        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(senderIds, cancellationToken);

        return page
            .Select(m =>
            {
                userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) sender);
                return ChatMapper.MapMessage(m, sender.FirstName, sender.LastName);
            })
            .ToList();
    }
}
