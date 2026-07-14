using Entities.Enums;

namespace CQRS.AI.Shared
{
    internal static class AICostImportItemStatusHelper
    {
        public static bool IsReviewable(AICostImportItemStatus status)
        {
            return status is AICostImportItemStatus.Pending
                or AICostImportItemStatus.ErrorNeedsReview
                or AICostImportItemStatus.DuplicateDetected;
        }

        public static bool IsDuplicateMatchSource(AICostImportItemStatus status)
        {
            return status is AICostImportItemStatus.Pending
                or AICostImportItemStatus.ErrorNeedsReview
                or AICostImportItemStatus.DuplicateDetected
                or AICostImportItemStatus.Accepted;
        }
    }
}
