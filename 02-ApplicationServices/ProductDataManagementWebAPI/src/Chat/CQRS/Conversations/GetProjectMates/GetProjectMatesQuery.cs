using Chat.DTOs;
using CQRS;

namespace Chat.CQRS.Conversations.GetProjectMates;

/// <summary>
/// Returns all projects the current user is a member of, each with project/tenant metadata
/// and the list of other members. Intended for populating contact lists when creating chats.
/// </summary>
public sealed record GetProjectMatesQuery() : IRequestQuery<List<ProjectContactsGroupWeb>>;
