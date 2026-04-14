using Entities.Models.CostTrackers;
using Microsoft.AspNetCore.Http;

namespace Business.Interfaces.Services
{
    public interface ICostTrackerAttachmentService
    {
        /// <summary>
        /// Synchronizuje załączniki kosztu.
        /// Usuwa (soft-delete + blob) załączniki których Id nie ma w existingAttachmentIds.
        /// Uploaduje nowe pliki i tworzy rekordy TrackedCostAttachment.
        /// Zwraca aktualną listę aktywnych załączników.
        /// </summary>
        Task<List<TrackedCostAttachment>> SyncAttachmentsAsync(
            TrackedCost cost,
            IReadOnlyList<IFormFile>? newFiles,
            IReadOnlyList<Guid> existingAttachmentIds,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generuje SAS URL dla załącznika.
        /// </summary>
        string GenerateFileUrl(TrackedCostAttachment attachment);
    }
}
