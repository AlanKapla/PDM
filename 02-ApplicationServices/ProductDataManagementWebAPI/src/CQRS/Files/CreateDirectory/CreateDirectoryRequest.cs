namespace CQRS.Files.CreateDirectory
{
    public sealed record CreateDirectoryRequest
    {
        public required string DirectoryName { get; init; }
        public Guid? ParentId { get; init; }
    }
}
