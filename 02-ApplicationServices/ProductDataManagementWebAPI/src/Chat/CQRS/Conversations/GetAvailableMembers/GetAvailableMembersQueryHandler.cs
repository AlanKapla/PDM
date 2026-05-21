using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using Entities.Models.Chats;
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.GetAvailableMembers;

public sealed class GetAvailableMembersQueryHandler : IRequestHandler<GetAvailableMembersQuery, List<AvailableMemberWeb>>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public GetAvailableMembersQueryHandler(
        IReadRepository<ChatModel> chatRepo,
        IReadRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        ICurrentUser currentUser)
    {
        this.chatRepo = chatRepo;
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.currentUser = currentUser;
    }

    public async Task<List<AvailableMemberWeb>> Handle(GetAvailableMembersQuery request, CancellationToken cancellationToken)
    {
        ChatModel chat = await GetAndValidateChatAsync(request.TenantId, request.ChatId, cancellationToken);

        if (!chat.IsGroupChat)
        {
            throw new ValidationApiException("Available members can only be retrieved for group chats.");
        }

        if (!chat.ProjectId.HasValue)
        {
            throw new ValidationApiException("This group chat has no associated project.");
        }

        await EnsureRequesterIsMemberAsync(request.ChatId, cancellationToken);

        IEnumerable<ChatMember> currentMembers = await chatMemberRepo.GetBySearch(
            cm => cm.ChatId == request.ChatId);

        List<Guid> currentMemberIds = currentMembers.Select(cm => cm.UserId).ToList();

        List<(Guid UserId, string FirstName, string LastName)> candidates =
            await projectMemberService.GetProjectMembersExcludingAsync(
                chat.ProjectId.Value, currentMemberIds, cancellationToken);

        return candidates
            .Select(c => new AvailableMemberWeb(c.UserId, c.FirstName, c.LastName))
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToList();
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

    private async Task EnsureRequesterIsMemberAsync(Guid chatId, CancellationToken cancellationToken)
    {
        bool isMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == chatId && cm.UserId == currentUser.Id,
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }
    }
}
