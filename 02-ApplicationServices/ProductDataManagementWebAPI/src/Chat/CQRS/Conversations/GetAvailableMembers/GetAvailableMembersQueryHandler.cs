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
using ChatModel = Entities.Models.Chats.Chat;
using MediatR;
using Repositories.Repository.Interfaces;

namespace Chat.CQRS.Conversations.GetAvailableMembers;

public sealed class GetAvailableMembersQueryHandler : IRequestHandler<GetAvailableMembersQuery, List<AvailableMemberWeb>>
{
    private readonly IReadRepository<ChatModel> chatRepo;
    private readonly IRepository<ChatMember> chatMemberRepo;
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public GetAvailableMembersQueryHandler(
        IReadRepository<ChatModel> chatRepo,
        IRepository<ChatMember> chatMemberRepo,
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
        ChatModel? chat = await chatRepo.GetById(request.ChatId);

        if (chat == null)
        {
            throw new NotFoundApiException("Chat", request.ChatId.ToString());
        }

        if (!chat.IsGroupChat)
        {
            throw new ValidationApiException("Available members can only be retrieved for group chats.");
        }

        if (!chat.ProjectId.HasValue)
        {
            throw new ValidationApiException("This group chat has no associated project.");
        }

        bool isMember = await chatMemberRepo.AnyAsync(
            cm => cm.ChatId == request.ChatId && cm.UserId == currentUser.Id,
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenApiException("You are not a member of this chat.");
        }

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
}
