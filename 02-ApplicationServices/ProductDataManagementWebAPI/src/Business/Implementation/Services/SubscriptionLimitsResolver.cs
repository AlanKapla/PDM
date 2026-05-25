using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class SubscriptionLimitsResolver : ISubscriptionLimitsResolver
    {
        private readonly IReadRepository<TenantSubscription> subscriptionRepo;
        private readonly ILogger<SubscriptionLimitsResolver> logger;

        public SubscriptionLimitsResolver(
            IReadRepository<TenantSubscription> subscriptionRepo,
            ILogger<SubscriptionLimitsResolver> logger)
        {
            this.subscriptionRepo = subscriptionRepo;
            this.logger = logger;
        }

        public async Task<SubscriptionLimits> ResolveAsync(Guid tenantId, CancellationToken ct = default)
        {
            TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
                s => s.TenantId == tenantId,
                ct,
                q => q.Include(s => s.Overrides));

            if (subscription is null)
            {
                throw new NotFoundApiException(nameof(TenantSubscription), tenantId.ToString());
            }

            if (subscription.IsFullAccess)
            {
                return SubscriptionLimits.FullAccess();
            }

            int maxProjects = subscription.MaxProjects;
            int maxUsers    = subscription.MaxUsers;

            foreach (SubscriptionOverride subscriptionOverride in subscription.Overrides)
            {
                if (!subscriptionOverride.IsValid())
                {
                    continue;
                }

                if (subscriptionOverride.Key == SubscriptionOverride.Keys.MaxProjects)
                {
                    if (!TryParseOverrideValue(subscriptionOverride, out int parsedProjects))
                    {
                        continue;
                    }

                    maxProjects = parsedProjects;
                }
                else if (subscriptionOverride.Key == SubscriptionOverride.Keys.MaxUsers)
                {
                    if (!TryParseOverrideValue(subscriptionOverride, out int parsedUsers))
                    {
                        continue;
                    }

                    maxUsers = parsedUsers;
                }
            }

            return new SubscriptionLimits
            {
                MaxProjects = maxProjects,
                MaxUsers    = maxUsers
            };
        }

        private bool TryParseOverrideValue(SubscriptionOverride subscriptionOverride, out int result)
        {
            try
            {
                result = int.Parse(subscriptionOverride.Value);
                return true;
            }
            catch (FormatException ex)
            {
                logger.LogWarning(
                    ex,
                    "Nie można sparsować wartości override [{Key}={Value}] dla subskrypcji {SubscriptionId}. Override zostanie pominięty.",
                    subscriptionOverride.Key,
                    subscriptionOverride.Value,
                    subscriptionOverride.TenantSubscriptionId);

                result = 0;
                return false;
            }
        }
    }
}
