namespace Business.Interfaces.Services;

public interface ITechnicalDocumentationProcessingService
{
    Task ProcessAsync(
        Guid documentationId,
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken);
}
