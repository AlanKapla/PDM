namespace Business.Interfaces.WebModels.Tenants
{
    /// <summary>
    /// Tenant details
    /// </summary>
    public sealed record TenantDetailsWeb
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required bool IsActive { get; init; }
        public required bool IsAdmin { get; init; }
        public List<TenantMemberWeb> Members { get; init; } = new();
        public List<TenantInvitationWeb> Invitations { get; init; } = new();
    }
}
