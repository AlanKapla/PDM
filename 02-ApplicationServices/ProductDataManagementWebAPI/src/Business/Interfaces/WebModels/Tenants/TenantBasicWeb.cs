namespace Business.Interfaces.WebModels.Tenants
{
    public sealed record TenantBasicWeb
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required bool IsActive { get; init; }
        public required bool IsAdmin { get; init; }
    }
}
