namespace Business.Interfaces.Services;

public interface IQueuedTechnicalDocumentationSender
{
    Task EnqueueAsync(
        Guid documentationId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        bool isManualRetry,
        CancellationToken cancellationToken);
}
