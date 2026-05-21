using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;

namespace CQRS.CostEstimates
{
    /// <summary>
    /// Wspólne strażniki autoryzacji dla handlerów modyfikujących strukturę kosztorysu
    /// (grupy, pozycje, kolejność, przeniesienia, usunięcia).
    /// Eliminuje duplikację 3 identycznych ifów w wielu handlerach.
    /// </summary>
    public static class CostEstimateAccessLevelExtensions
    {
        public static void EnsureCanModifyStructure(this CostEstimateAccessLevel level)
        {
            if (level == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
            }

            if (level == CostEstimateAccessLevel.Restricted)
            {
                throw new ForbiddenApiException("Shared users cannot modify the cost estimate structure.");
            }

            if (level == CostEstimateAccessLevel.ReadOnly)
            {
                throw new ForbiddenApiException("Read-only access does not allow modifying the cost estimate structure.");
            }
        }
    }
}
