using Entities.Enums;
using Entities.Models.Tenants;

namespace Business.Interfaces.WebModels.Projects;

public sealed record ProjectInvitationWeb
{
    public required Guid InvitationId { get; init; }
    public required Guid TenantId { get; init; }
    public required string TenantName { get; init; }
    public required Guid ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public required string Email { get; init; }
    public required bool IsAdmin { get; init; }
    public required IReadOnlyList<ProjectModule> Modules { get; init; }
    public required string InvitedByUserEmail { get; init; }
    public required string InvitedByUserName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? ExpiresAt { get; init; }
    public required InvitationStatus Status { get; init; }
    public required string Token { get; init; }
}
