using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Chats;
using MediatR;

namespace Chat.CQRS.Conversations.GetProjectMates;

public sealed class GetProjectMatesQueryHandler : IRequestHandler<GetProjectMatesQuery, List<ProjectContactsGroupWeb>>
{
    private readonly IProjectMemberService projectMemberService;
    private readonly ICurrentUser currentUser;

    public GetProjectMatesQueryHandler(
        IProjectMemberService projectMemberService,
        ICurrentUser currentUser)
    {
        this.projectMemberService = projectMemberService;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectContactsGroupWeb>> Handle(GetProjectMatesQuery request, CancellationToken cancellationToken)
    {
        List<ProjectMembersGroupDto> groups = await projectMemberService.GetUserProjectGroupsAsync(
            currentUser.Id,
            cancellationToken);

        if (request.TenantId is not null)
        {
            groups = groups.Where(g => g.TenantId == request.TenantId.Value).ToList();
        }

        if (groups.Count == 0)
        {
            return new();
        }

        HashSet<Guid> allUserIds = groups.SelectMany(g => g.MemberUserIds).ToHashSet();

        Dictionary<Guid, (string FirstName, string LastName)> userNames =
            await projectMemberService.GetUserNamesByIdsAsync(allUserIds, cancellationToken);

        return groups
            .Select(g => new ProjectContactsGroupWeb(
                ProjectId: g.ProjectId,
                ProjectName: g.ProjectName,
                TenantId: g.TenantId,
                TenantName: g.TenantName,
                Members: g.MemberUserIds
                    .Select(id =>
                    {
                        userNames.TryGetValue(id, out (string FirstName, string LastName) n);
                        return new ProjectMateWeb(id, n.FirstName ?? string.Empty, n.LastName ?? string.Empty);
                    })
                    .OrderBy(m => m.LastName)
                    .ThenBy(m => m.FirstName)
                    .ToList()))
            .OrderBy(g => g.TenantName)
            .ThenBy(g => g.ProjectName)
            .ToList();
    }
}
