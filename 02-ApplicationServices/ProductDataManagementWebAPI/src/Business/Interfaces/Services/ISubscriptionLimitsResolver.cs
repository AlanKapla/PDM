using Entities.Models.Subscriptions;

namespace Business.Interfaces.Services
{
    public interface ISubscriptionLimitsResolver
    {
        Task<SubscriptionLimits> ResolveAsync(Guid tenantId, CancellationToken ct = default);
    }
}
