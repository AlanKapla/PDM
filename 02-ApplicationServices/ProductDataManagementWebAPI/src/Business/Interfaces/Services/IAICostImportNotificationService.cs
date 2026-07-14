using Entities.Models.AI;

namespace Business.Interfaces.Services
{
    public interface IAICostImportNotificationService
    {
        Task NotifyBatchCompletedAsync(
            AICostImportBatch batch,
            CancellationToken cancellationToken);
    }
}
