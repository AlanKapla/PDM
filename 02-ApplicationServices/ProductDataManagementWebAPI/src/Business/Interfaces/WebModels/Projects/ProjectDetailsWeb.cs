namespace Business.Interfaces.WebModels.Projects
{
    public sealed record ProjectDetailsWeb
    {
        public required Guid Id { get; init; }
        public required Guid TenantId { get; init; }
        public required string Name { get; init; }
        public required bool IsActive { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required Guid CreatedByUserId { get; init; }
        public required string CreatedByUserName { get; init; }
        public required bool IsAdmin { get; init; }
        public required bool CanViewAllResources { get; init; }
        public required int MembersCount { get; init; }
        public required IReadOnlySet<string> UserPermissions { get; init; }
        public ProjectCurrencyWeb? Currency { get; init; }
    }
}
