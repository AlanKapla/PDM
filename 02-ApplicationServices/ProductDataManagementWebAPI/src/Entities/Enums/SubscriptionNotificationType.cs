namespace Entities.Enums
{
    public enum SubscriptionNotificationType
    {
        /// <summary>Przypomnienie 7 dni przed terminem płatności.</summary>
        PaymentDueSoon      = 0,

        /// <summary>Potwierdzenie udanej płatności.</summary>
        PaymentSucceeded    = 1,

        /// <summary>Informacja o nieudanej płatności.</summary>
        PaymentFailed       = 2,

        /// <summary>Ostrzeżenie o wejściu w grace period.</summary>
        GracePeriodStarted  = 3,

        /// <summary>Informacja o zablokowaniu subskrypcji.</summary>
        SubscriptionPastDue = 4
    }
}
