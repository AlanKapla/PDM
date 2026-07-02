namespace Business.Interfaces.Services;

public interface ITechnicalDocumentationImagePreprocessor
{
    Task<(byte[] OptimizedBytes, string OptimizedMediaType)> PrepareForVisionAsync(
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken);
}
