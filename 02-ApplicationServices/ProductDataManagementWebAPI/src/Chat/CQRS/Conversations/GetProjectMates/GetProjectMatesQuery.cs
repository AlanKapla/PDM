using Business.Interfaces.WebModels.Chats;
using CQRS;

namespace Chat.CQRS.Conversations.GetProjectMates;

/// <summary>
/// Returns all projects the current user is a member of, each with project/tenant metadata
/// and the list of other members. Intended for populating contact lists when creating chats.
/// When <paramref name="TenantId"/> is set, results are restricted to that tenant.
/// </summary>
public sealed record GetProjectMatesQuery(Guid? TenantId = null)
    : IRequestQuery<List<ProjectContactsGroupWeb>>;
