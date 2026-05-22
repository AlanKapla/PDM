using Entities.Enums;

namespace Business.Interfaces.WebModels.Projects
{
    /// <summary>
    /// Project member details with role code instead of enum
    /// </summary>
    public sealed record ProjectMemberWeb
    {
        public required Guid UserId { get; init; }
        public required string Email { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string RoleCode { get; init; }
        public required DateTime JoinedAt { get; init; }
    }
}
