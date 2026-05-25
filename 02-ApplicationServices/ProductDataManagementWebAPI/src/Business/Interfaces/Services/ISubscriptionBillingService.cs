using Entities.Enums;
using Entities.Models.Subscriptions;

namespace Business.Interfaces.Services
{
    public interface ISubscriptionBillingService
    {
        /// <summary>
        /// Mock płatności — symuluje udaną płatność za subskrypcję.
        /// Tworzy SubscriptionPayment, aktualizuje TenantSubscription, zapisuje notyfikacje dla adminów tenanta.
        /// </summary>
        Task<SubscriptionPayment> ProcessMockPaymentAsync(
            Guid tenantId,
            CancellationToken ct = default);

        /// <summary>
        /// Inicjalizuje billing dla nowej płatnej subskrypcji.
        /// Wywołaj przy zmianie planu z Free na płatny.
        /// </summary>
        Task InitializeBillingAsync(
            Guid tenantId,
            SubscriptionPlan newPlan,
            CancellationToken ct = default);
    }
}
