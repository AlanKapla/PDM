using Entities.Models.Costs;
using Microsoft.AspNetCore.Http;

namespace Business.Interfaces.Services
{
    public interface ICostTrackerAttachmentService
    {
        /// <summary>
        /// Synchronizuje załączniki kosztu.
        /// Usuwa (soft-delete + blob) załączniki których Id nie ma w existingAttachmentIds.
        /// Gdy existingAttachmentIds jest null, istniejące załączniki nie są usuwane.
        /// Uploaduje nowe pliki i tworzy rekordy BaseCostAttachment.
        /// Zwraca aktualną listę aktywnych załączników.
        /// </summary>
        Task<List<BaseCostAttachment>> SyncAttachmentsAsync(
            BaseCost cost,
            IReadOnlyList<IFormFile>? newFiles,
            IReadOnlyList<Guid>? existingAttachmentIds,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generuje SAS URL dla załącznika.
        /// </summary>
        string GenerateFileUrl(BaseCostAttachment attachment);
    }
}
