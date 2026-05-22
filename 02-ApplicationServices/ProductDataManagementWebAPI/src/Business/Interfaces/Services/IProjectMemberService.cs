using Business.Interfaces.DTO;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;

namespace Business.Interfaces.Services;

public interface IProjectMemberService
{
    /// <summary>
    /// Returns a ProjectMember record (with TenantId + ProjectId) for the first project
    /// shared between the two users. Returns null if no shared project exists.
    /// </summary>
    Task<ProjectMember?> FindSharedProjectAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a ProjectMember record for the first project where ALL provided user IDs
    /// are members. Returns null if no such common project exists.
    /// </summary>
    Task<ProjectMember?> FindCommonProjectForAllAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the user is a member of the specified project.
    /// </summary>
    Task<bool> IsUserInProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full display name of a user. Falls back to userId.ToString()
    /// if the user cannot be resolved.
    /// </summary>
    Task<string> GetUserDisplayNameAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns first and last names for a set of user IDs.
    /// Missing users are simply absent from the result dictionary.
    /// </summary>
    Task<Dictionary<Guid, (string FirstName, string LastName)>> GetUserNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all projects the user is a member of, each with project/tenant metadata
    /// and the list of other member user IDs in that project.
    /// </summary>
    Task<List<ProjectMembersGroupDto>> GetUserProjectGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if ALL provided user IDs are members of the specified project.
    /// </summary>
    Task<bool> AreAllMembersOfProjectAsync(
        Guid projectId,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns members of the specified project who are NOT in the excludeUserIds set,
    /// along with their first and last names.
    /// </summary>
    Task<List<(Guid UserId, string FirstName, string LastName)>> GetProjectMembersExcludingAsync(
        Guid projectId,
        IEnumerable<Guid> excludeUserIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the TenantId of the specified project, or null if the project does not exist.
    /// </summary>
    Task<Guid?> GetProjectTenantIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
