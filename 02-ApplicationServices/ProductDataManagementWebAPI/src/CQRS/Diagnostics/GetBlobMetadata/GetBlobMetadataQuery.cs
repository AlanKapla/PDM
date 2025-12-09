using MediatR;

namespace CQRS.Diagnostics.GetBlobMetadata
{
    /// <summary>
    /// Query do pobierania metadanych bloba (diagnostyka)
    /// </summary>
    public record GetBlobMetadataQuery : IRequest<BlobMetadataResult>
    {
        public required string BlobPath { get; init; }
    }

    public record BlobMetadataResult
    {
        public string BlobPath { get; init; } = string.Empty;
        public bool Exists { get; init; }
        public string? ContentType { get; init; }
        public string? ContentDisposition { get; init; }
        public long? ContentLength { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public string? ETag { get; init; }
    }
}
