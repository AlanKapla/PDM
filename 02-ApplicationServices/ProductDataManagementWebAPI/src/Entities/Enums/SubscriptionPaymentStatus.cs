namespace Entities.Enums
{
    public enum SubscriptionPaymentStatus
    {
        /// <summary>Płatność oczekująca (mock: zaraz zostanie zatwierdzona).</summary>
        Pending   = 0,

        /// <summary>Płatność zakończona sukcesem.</summary>
        Succeeded = 1,

        /// <summary>Płatność nieudana.</summary>
        Failed    = 2,

        /// <summary>Płatność zwrócona.</summary>
        Refunded  = 3
    }
}
