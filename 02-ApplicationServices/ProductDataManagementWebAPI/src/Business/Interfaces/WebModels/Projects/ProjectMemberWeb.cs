namespace Business.Interfaces.WebModels.Projects
{
    public sealed record ProjectMemberWeb
    {
        public required Guid UserId { get; init; }
        public required string Email { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public string? CompanyName { get; init; }
        public required DateTime JoinedAt { get; init; }
        public required bool IsAdmin { get; init; }
        public IReadOnlyList<int> Modules { get; init; } = Array.Empty<int>();
    }
}
