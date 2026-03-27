using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Chat.DTOs;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.GetChatMembers;

public sealed class GetChatMembersQueryHandler : IRequestHandler<GetChatMembersQuery, List<ChatMemberWeb>>
{
    private readonly IReadRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public GetChatMembersQueryHandler(
        IReadRepository<ChatMember> chatMemberRepo,
        IProjectMemberService projectMemberService,
        ICurrentUser currentUser)
    {
        this.chatMemberRepo = chatMemberRepo;
        this.projectMemberService = projectMemberService;
        this.currentUser = currentUser;
    }

    public async Task<List<ChatMemberWeb>> Handle(GetChatMembersQuery request, CancellationToken cancellationToken)
    {
        bool requesterIsMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id,
            cancellationToken);

        if (!requesterIsMember)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

        IEnumerable<ChatMember> members = await chatMemberRepo.GetBySearch(
            cm => cm.ChatId == request.ChatId);

        HashSet<Guid> memberIds = members.Select(m => m.UserId).ToHashSet();

        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(memberIds, cancellationToken);

        return members
            .Select(m =>
            {
                userNames.TryGetValue(m.UserId, out (string FirstName, string LastName) name);
                return new ChatMemberWeb(
                    UserId: m.UserId,
                    FirstName: name.FirstName ?? string.Empty,
                    LastName: name.LastName ?? string.Empty,
                    JoinedAt: m.JoinedAt,
                    IsAdmin: m.IsAdmin,
                    LastReadAt: m.LastReadAt);
            })
            .ToList();
    }
}
