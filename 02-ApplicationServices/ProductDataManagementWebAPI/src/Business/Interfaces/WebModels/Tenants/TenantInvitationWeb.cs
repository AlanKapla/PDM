using Entities.Enums;
using Entities.Models.Tenants;

namespace Business.Interfaces.WebModels.Tenants
{
    public sealed record TenantInvitationWeb
    {
        public required Guid InvitationId { get; init; }
        public required Guid TenantId { get; init; }
        public required string TenantName { get; init; }
        public Guid? ProjectId { get; init; }
        public string? ProjectName { get; init; }
        public required string Email { get; init; }
        public required bool IsAdmin { get; init; }
        public IReadOnlyList<ProjectModule> Modules { get; init; } = Array.Empty<ProjectModule>();
        public required string InvitedByUserEmail { get; init; }
        public required string InvitedByUserName { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime? ExpiresAt { get; init; }
        public required InvitationStatus Status { get; init; }
        public required string Token { get; init; }
    }
}
