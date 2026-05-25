using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.Subscriptions
{
    /// <summary>
    /// Rekord każdej próby płatności za subskrypcję.
    /// Mock — brak integracji z bramką płatności.
    /// </summary>
    public class SubscriptionPayment : BaseEntity
    {
        public Guid TenantSubscriptionId { get; set; }
        public Guid TenantId { get; set; }

        /// <summary>Plan za który zapłacono.</summary>
        public SubscriptionPlan Plan { get; set; }

        /// <summary>Kwota płatności.</summary>
        public decimal Amount { get; set; }

        /// <summary>Waluta (np. "PLN").</summary>
        public string Currency { get; set; } = "PLN";

        public SubscriptionPaymentStatus Status { get; set; }

        /// <summary>Za jaki okres płatność (snapshot daty).</summary>
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        /// <summary>Data wykonania płatności. NULL gdy pending.</summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>Opcjonalny zewnętrzny ID transakcji (dla przyszłej integracji z bramką).</summary>
        public string? ExternalTransactionId { get; set; }

        /// <summary>Powód błędu gdy Status = Failed.</summary>
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual TenantSubscription TenantSubscription { get; set; } = default!;
    }
}
