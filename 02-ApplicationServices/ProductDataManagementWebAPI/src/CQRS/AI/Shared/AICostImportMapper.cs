using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Enums;
using Entities.Models.AI;
using EntityCostDocumentType = Entities.Enums.CostDocumentType;

namespace CQRS.AI.Shared
{
    internal static class AICostImportMapper
    {
        public static EntityCostDocumentType ToEntityCostDocumentType(ParseCostDocument.CostDocumentType costType)
        {
            return costType == ParseCostDocument.CostDocumentType.ProjectCost
                ? EntityCostDocumentType.ProjectCost
                : EntityCostDocumentType.TrackedCost;
        }

        public static string ToCostDocumentTypeString(EntityCostDocumentType costType)
        {
            return costType == EntityCostDocumentType.ProjectCost
                ? nameof(ParseCostDocument.CostDocumentType.ProjectCost)
                : nameof(ParseCostDocument.CostDocumentType.TrackedCost);
        }

        public static AICostImportItemWeb MapItemToWeb(
            AICostImportItem item,
            AICostImportBatch batch,
            IAICostImportBlobService blobService)
        {
            ParsedCostDto? parsedData = DeserializeParsedData(item.ParsedDataJson);
            TrackedCostContextDto? trackedContext = DeserializeTrackedContext(batch.TrackedCostContextJson);

            string? previewUrl = null;
            if (AICostImportItemStatusHelper.IsReviewable(item.Status))
            {
                previewUrl = blobService.GeneratePendingPreviewUrl(item.BlobPath, item.OriginalFileName);
            }

            return new AICostImportItemWeb
            {
                Id = item.Id,
                BatchId = item.BatchId,
                TenantId = item.TenantId,
                ProjectId = item.ProjectId,
                Status = item.Status.ToString(),
                OriginalFileName = item.OriginalFileName,
                ContentType = item.ContentType,
                FileSizeBytes = item.FileSizeBytes,
                ParsedData = parsedData,
                LastError = item.LastError,
                AnalyzedAt = item.AnalyzedAt,
                PreviewUrl = previewUrl,
                CostDocumentType = ToCostDocumentTypeString(batch.CostDocumentType),
                TrackedCostContext = trackedContext,
                CreatedAt = item.CreatedAt
            };
        }

        private static ParsedCostDto? DeserializeParsedData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ParsedCostDto>(json);
            }
            catch
            {
                return null;
            }
        }

        private static TrackedCostContextDto? DeserializeTrackedContext(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<TrackedCostContextDto>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
