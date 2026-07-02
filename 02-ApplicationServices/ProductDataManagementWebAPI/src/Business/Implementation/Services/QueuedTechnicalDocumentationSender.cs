using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services;

public sealed class QueuedTechnicalDocumentationSender : IQueuedTechnicalDocumentationSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IQueueStorageService queueStorageService;
    private readonly ILogger<QueuedTechnicalDocumentationSender> logger;

    public QueuedTechnicalDocumentationSender(
        IQueueStorageService queueStorageService,
        ILogger<QueuedTechnicalDocumentationSender> logger)
    {
        this.queueStorageService = queueStorageService;
        this.logger = logger;
    }

    public async Task EnqueueAsync(
        Guid documentationId,
        Guid tenantId,
        Guid projectId,
        Guid userId,
        bool isManualRetry,
        CancellationToken cancellationToken)
    {
        await queueStorageService.EnsureQueueAsync(QueueNames.TechnicalDocumentationProcess, cancellationToken);

        TechnicalDocumentationQueueMessageDto message = new()
        {
            DocumentationId = documentationId,
            TenantId = tenantId,
            ProjectId = projectId,
            UserId = userId,
            IsManualRetry = isManualRetry
        };

        string payload = JsonSerializer.Serialize(message, JsonOptions);
        await queueStorageService.EnqueueAsync(
            QueueNames.TechnicalDocumentationProcess,
            payload,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Enqueued technical documentation {DocumentationId} for project {ProjectId} (manualRetry={IsManualRetry})",
            documentationId, projectId, isManualRetry);
    }
}
