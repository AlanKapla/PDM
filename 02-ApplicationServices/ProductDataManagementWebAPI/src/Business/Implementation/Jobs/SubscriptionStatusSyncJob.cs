using Business.Interfaces.Constants;
using Entities.Enums;
using Entities.Models.Subscriptions;
using Entities.Models.Tenants;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Jobs
{
    /// <summary>
    /// Sprawdza wszystkie płatne subskrypcje i aktualizuje statusy na podstawie dat.
    /// TODO: Zarejestrować jako job cykliczny (Hangfire / Quartz) uruchamiany codziennie.
    /// </summary>
    public sealed class SubscriptionStatusSyncJob
    {
        private readonly IReadRepository<TenantSubscription> subscriptionReadRepo;
        private readonly IRepository<TenantSubscription> subscriptionRepo;
        private readonly IRepository<SubscriptionNotification> notificationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;

        public SubscriptionStatusSyncJob(
            IReadRepository<TenantSubscription> subscriptionReadRepo,
            IRepository<TenantSubscription> subscriptionRepo,
            IRepository<SubscriptionNotification> notificationRepo,
            IRepository<TenantMember> tenantMemberRepo)
        {
            this.subscriptionReadRepo = subscriptionReadRepo;
            this.subscriptionRepo     = subscriptionRepo;
            this.notificationRepo     = notificationRepo;
            this.tenantMemberRepo     = tenantMemberRepo;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            DateTime now = DateTime.UtcNow;

            IEnumerable<TenantSubscription> subscriptions = await subscriptionReadRepo.GetBySearch(
                s => s.Plan != SubscriptionPlan.Free
                     && s.Status != SubscriptionStatus.Canceled
                     && s.NextPaymentDue != null);

            foreach (TenantSubscription subscription in subscriptions)
            {
                await ProcessSubscriptionAsync(subscription, now, ct);
            }

            await subscriptionRepo.SaveChangesAsync(ct);
        }

        private async Task ProcessSubscriptionAsync(
            TenantSubscription subscription,
            DateTime now,
            CancellationToken ct)
        {
            if (subscription.GracePeriodEndsAt.HasValue
                && now > subscription.GracePeriodEndsAt.Value
                && subscription.Status != SubscriptionStatus.PastDue)
            {
                subscription.MarkAsPastDue();
                await CreateNotificationsAsync(
                    subscription, SubscriptionNotificationType.SubscriptionPastDue,
                    "Subskrypcja zablokowana",
                    $"Twoja subskrypcja została zablokowana z powodu braku płatności po upływie okresu karencji.",
                    ct);
            }
            else if (subscription.NextPaymentDue.HasValue
                     && now > subscription.NextPaymentDue.Value
                     && subscription.Status == SubscriptionStatus.Active)
            {
                subscription.MarkAsGracePeriod();
                await CreateNotificationsAsync(
                    subscription, SubscriptionNotificationType.GracePeriodStarted,
                    "Rozpoczął się okres karencji",
                    $"Termin płatności minął. Masz czas do {subscription.GracePeriodEndsAt:dd.MM.yyyy} na uregulowanie płatności.",
                    ct);
            }
            else if (subscription.NextPaymentDue.HasValue
                     && now >= subscription.NextPaymentDue.Value.AddDays(-7)
                     && now < subscription.NextPaymentDue.Value
                     && subscription.Status == SubscriptionStatus.Active)
            {
                await CreatePaymentDueSoonNotificationIfNeededAsync(subscription, ct);
            }
        }

        private async Task CreatePaymentDueSoonNotificationIfNeededAsync(
            TenantSubscription subscription,
            CancellationToken ct)
        {
            bool alreadyNotified = await notificationRepo.AnyAsync(
                n => n.TenantId == subscription.TenantId
                     && n.Type == SubscriptionNotificationType.PaymentDueSoon
                     && n.CreatedAt >= subscription.CurrentPeriodStart,
                ct);

            if (alreadyNotified)
            {
                return;
            }

            await CreateNotificationsAsync(
                subscription, SubscriptionNotificationType.PaymentDueSoon,
                "Zbliża się termin płatności",
                $"Twoja płatność za subskrypcję jest wymagana do {subscription.NextPaymentDue:dd.MM.yyyy}.",
                ct);
        }

        private async Task CreateNotificationsAsync(
            TenantSubscription subscription,
            SubscriptionNotificationType type,
            string subject,
            string body,
            CancellationToken ct)
        {
            List<string> adminEmails = await tenantMemberRepo.SelectAsync(
                m => m.TenantId == subscription.TenantId
                     && m.IsActive
                     && m.MemberRole!.Code == RoleCodes.TenantAdmin,
                m => m.User.Email,
                ct);

            foreach (string email in adminEmails)
            {
                SubscriptionNotification notification = new()
                {
                    TenantId       = subscription.TenantId,
                    Type           = type,
                    RecipientEmail = email,
                    Subject        = subject,
                    Body           = body,
                    SentAt         = DateTime.UtcNow,
                    CreatedAt      = DateTime.UtcNow
                };
                await notificationRepo.Insert(notification);
            }
        }
    }
}
