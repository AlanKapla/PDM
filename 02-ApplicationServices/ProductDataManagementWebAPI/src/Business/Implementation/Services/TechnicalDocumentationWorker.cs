using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Entities.Enums;
using Entities.Models.TechnicalDocumentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class TechnicalDocumentationWorker : BackgroundService
{
    private const int MaxDequeueCount = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IQueueStorageService queueStorage;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TechnicalDocumentationWorker> logger;

    public TechnicalDocumentationWorker(
        IQueueStorageService queueStorage,
        IServiceScopeFactory scopeFactory,
        ILogger<TechnicalDocumentationWorker> logger)
    {
        this.queueStorage = queueStorage;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await queueStorage.EnsureQueueAsync(QueueNames.TechnicalDocumentationProcess, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DequeuedMessage? message = await queueStorage.DequeueAsync(
                    QueueNames.TechnicalDocumentationProcess,
                    cancellationToken: stoppingToken);

                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                    continue;
                }

                if (message.DequeueCount > MaxDequeueCount)
                {
                    await HandlePoisonMessageAsync(message, stoppingToken);
                    continue;
                }

                await ProcessMessageAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error processing technical documentation queue message");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(DequeuedMessage message, CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        TechnicalDocumentationQueueMessageDto? payload = JsonSerializer.Deserialize<TechnicalDocumentationQueueMessageDto>(
            message.Text, JsonOptions);

        if (payload is null)
        {
            logger.LogWarning("Received invalid technical documentation message: {MessageId}", message.MessageId);
            await queueStorage.DeleteMessageAsync(
                QueueNames.TechnicalDocumentationProcess,
                message.MessageId,
                message.PopReceipt,
                stoppingToken);
            return;
        }

        IRepository<ProjectTechnicalDocumentation> documentationRepository =
            scope.ServiceProvider.GetRequiredService<IRepository<ProjectTechnicalDocumentation>>();
        ITechnicalDocumentationProcessingService processingService =
            scope.ServiceProvider.GetRequiredService<ITechnicalDocumentationProcessingService>();

        if (!payload.IsManualRetry)
        {
            await IncrementAutoRetryCountAsync(
                documentationRepository, payload, stoppingToken);
        }

        try
        {
            await processingService.ProcessAsync(
                payload.DocumentationId,
                payload.TenantId,
                payload.ProjectId,
                stoppingToken);

            await queueStorage.DeleteMessageAsync(
                QueueNames.TechnicalDocumentationProcess,
                message.MessageId,
                message.PopReceipt,
                stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Technical documentation processing attempt failed for {DocumentationId} (dequeue {Count})",
                payload.DocumentationId, message.DequeueCount);
        }
    }

    private async Task HandlePoisonMessageAsync(DequeuedMessage message, CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        TechnicalDocumentationQueueMessageDto? payload = JsonSerializer.Deserialize<TechnicalDocumentationQueueMessageDto>(
            message.Text, JsonOptions);

        if (payload is not null)
        {
            IRepository<ProjectTechnicalDocumentation> documentationRepository =
                scope.ServiceProvider.GetRequiredService<IRepository<ProjectTechnicalDocumentation>>();
            ITechnicalDocumentationDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<ITechnicalDocumentationDispatcher>();

            ProjectTechnicalDocumentation? documentation = await documentationRepository.GetFirstBySearch(
                d => d.TenantId == payload.TenantId
                    && d.ProjectId == payload.ProjectId
                    && d.Id == payload.DocumentationId);

            if (documentation is not null)
            {
                documentation.Status = TechnicalDocumentationStatus.Failed;
                documentation.ErrorMessage = "Auto-retry limit exceeded";
                documentation.CompletedAt = DateTime.UtcNow;

                await documentationRepository.Update(documentation);
                await documentationRepository.SaveChangesAsync(stoppingToken);

                await dispatcher.DispatchCompletedAsync(new TechnicalDocumentationProcessingResultDto
                {
                    DocumentationId = documentation.Id,
                    ProjectId = documentation.ProjectId,
                    TenantId = documentation.TenantId,
                    Name = documentation.Name,
                    Status = documentation.Status,
                    ErrorMessage = documentation.ErrorMessage
                }, stoppingToken);
            }
        }

        logger.LogError(
            "Poison message detected after {Count} attempts. MessageId: {MessageId}. Deleting.",
            message.DequeueCount, message.MessageId);

        await queueStorage.DeleteMessageAsync(
            QueueNames.TechnicalDocumentationProcess,
            message.MessageId,
            message.PopReceipt,
            stoppingToken);
    }

    private static async Task IncrementAutoRetryCountAsync(
        IRepository<ProjectTechnicalDocumentation> documentationRepository,
        TechnicalDocumentationQueueMessageDto payload,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation? documentation = await documentationRepository.GetFirstBySearch(
            d => d.TenantId == payload.TenantId
                && d.ProjectId == payload.ProjectId
                && d.Id == payload.DocumentationId);

        if (documentation is null)
        {
            return;
        }

        documentation.AutoRetryCount++;
        await documentationRepository.Update(documentation);
        await documentationRepository.SaveChangesAsync(cancellationToken);
    }
}
