using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Enums;
using Entities.Models.AI;
using Entities.Models.Costs;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services.AI
{
    public sealed class AICostDuplicateDetectionService : IAICostDuplicateDetectionService
    {
        private readonly IReadRepository<AICostImportItem> importItemRepo;
        private readonly IReadRepository<BaseCost> costRepo;

        public AICostDuplicateDetectionService(
            IReadRepository<AICostImportItem> importItemRepo,
            IReadRepository<BaseCost> costRepo)
        {
            this.importItemRepo = importItemRepo;
            this.costRepo = costRepo;
        }

        public async Task<bool> IsDuplicateAsync(
            Guid tenantId,
            Guid projectId,
            string fileHashSha256,
            ParsedCostDto parsedData,
            Guid? excludeItemId,
            CancellationToken cancellationToken)
        {
            bool hashDuplicateInItems = await importItemRepo.AnyAsync(
                i => i.TenantId == tenantId
                     && i.ProjectId == projectId
                     && i.FileHashSha256 == fileHashSha256
                     && (i.Status == AICostImportItemStatus.Pending
                         || i.Status == AICostImportItemStatus.ErrorNeedsReview
                         || i.Status == AICostImportItemStatus.DuplicateDetected
                         || i.Status == AICostImportItemStatus.Accepted)
                     && (excludeItemId == null || i.Id != excludeItemId),
                cancellationToken);

            if (hashDuplicateInItems)
            {
                return true;
            }

            bool hashDuplicateInCosts = await costRepo.AnyAsync(
                c => c.TenantId == tenantId
                     && c.ProjectId == projectId
                     && c.SourceFileHashSha256 == fileHashSha256,
                cancellationToken);

            if (hashDuplicateInCosts)
            {
                return true;
            }

            if (!IsSecondaryMatchCandidate(parsedData))
            {
                return false;
            }

            DateTime dateDay = parsedData.Date!.Value.Date;

            bool secondaryDuplicateInItems = await importItemRepo.AnyAsync(
                i => i.TenantId == tenantId
                     && i.ProjectId == projectId
                     && i.ParsedDataJson != null
                     && (i.Status == AICostImportItemStatus.Pending
                         || i.Status == AICostImportItemStatus.ErrorNeedsReview
                         || i.Status == AICostImportItemStatus.DuplicateDetected
                         || i.Status == AICostImportItemStatus.Accepted)
                     && (excludeItemId == null || i.Id != excludeItemId),
                cancellationToken);

            if (secondaryDuplicateInItems)
            {
                IEnumerable<AICostImportItem> candidates = await importItemRepo.GetBySearch(
                    i => i.TenantId == tenantId
                         && i.ProjectId == projectId
                         && i.ParsedDataJson != null
                         && (i.Status == AICostImportItemStatus.Pending
                         || i.Status == AICostImportItemStatus.ErrorNeedsReview
                         || i.Status == AICostImportItemStatus.DuplicateDetected
                         || i.Status == AICostImportItemStatus.Accepted)
                         && (excludeItemId == null || i.Id != excludeItemId));

                foreach (AICostImportItem candidate in candidates)
                {
                    ParsedCostDto? candidateData = DeserializeParsedData(candidate.ParsedDataJson);
                    if (candidateData is not null && MatchesSecondary(parsedData, candidateData))
                    {
                        return true;
                    }
                }
            }

            IEnumerable<BaseCost> costs = await costRepo.GetBySearch(
                c => c.TenantId == tenantId
                     && c.ProjectId == projectId
                     && c.Net == parsedData.Net
                     && c.Date.HasValue
                     && c.Date.Value.Date == dateDay
                     && c.Number == parsedData.Number);

            foreach (BaseCost cost in costs)
            {
                if (MatchesSecondaryCost(parsedData, cost))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSecondaryMatchCandidate(ParsedCostDto parsedData)
        {
            return parsedData.Net.HasValue
                   && parsedData.Date.HasValue
                   && !string.IsNullOrWhiteSpace(parsedData.Number)
                   && (parsedData.ContractorId.HasValue
                       || !string.IsNullOrWhiteSpace(parsedData.ContractorNip));
        }

        private static bool MatchesSecondary(ParsedCostDto a, ParsedCostDto b)
        {
            if (!a.Net.HasValue || !b.Net.HasValue || a.Net != b.Net)
            {
                return false;
            }

            if (!a.Date.HasValue || !b.Date.HasValue || a.Date.Value.Date != b.Date.Value.Date)
            {
                return false;
            }

            if (!string.Equals(a.Number, b.Number, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (a.ContractorId.HasValue && b.ContractorId.HasValue)
            {
                return a.ContractorId == b.ContractorId;
            }

            return string.Equals(a.ContractorNip, b.ContractorNip, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesSecondaryCost(ParsedCostDto parsedData, BaseCost cost)
        {
            if (!parsedData.Net.HasValue || parsedData.Net != cost.Net)
            {
                return false;
            }

            if (!parsedData.Date.HasValue || !cost.Date.HasValue
                || parsedData.Date.Value.Date != cost.Date.Value.Date)
            {
                return false;
            }

            if (!string.Equals(parsedData.Number, cost.Number, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (parsedData.ContractorId.HasValue && cost.ContractorId.HasValue)
            {
                return parsedData.ContractorId == cost.ContractorId;
            }

            return false;
        }

        private static ParsedCostDto? DeserializeParsedData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ParsedCostDto>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
