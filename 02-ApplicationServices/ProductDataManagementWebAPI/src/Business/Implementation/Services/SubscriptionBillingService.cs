using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.Subscriptions;
using Entities.Models.Tenants;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class SubscriptionBillingService : ISubscriptionBillingService
    {
        private readonly IReadRepository<TenantSubscription> subscriptionReadRepo;
        private readonly IRepository<TenantSubscription> subscriptionRepo;
        private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;
        private readonly IRepository<SubscriptionPayment> paymentRepo;
        private readonly IRepository<SubscriptionNotification> notificationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;

        public SubscriptionBillingService(
            IReadRepository<TenantSubscription> subscriptionReadRepo,
            IRepository<TenantSubscription> subscriptionRepo,
            IReadRepository<SubscriptionPlanDefinition> planRepo,
            IRepository<SubscriptionPayment> paymentRepo,
            IRepository<SubscriptionNotification> notificationRepo,
            IRepository<TenantMember> tenantMemberRepo)
        {
            this.subscriptionReadRepo = subscriptionReadRepo;
            this.subscriptionRepo     = subscriptionRepo;
            this.planRepo             = planRepo;
            this.paymentRepo          = paymentRepo;
            this.notificationRepo     = notificationRepo;
            this.tenantMemberRepo     = tenantMemberRepo;
        }

        public async Task<SubscriptionPayment> ProcessMockPaymentAsync(
            Guid tenantId,
            CancellationToken ct = default)
        {
            TenantSubscription subscription = await GetAndValidatePaidSubscriptionAsync(tenantId, ct);

            bool isAlreadyPaid = subscription.LastPaidAt.HasValue
                && subscription.LastPaidAt.Value >= subscription.CurrentPeriodStart;

            if (isAlreadyPaid)
            {
                throw new ConflictApiException(
                    nameof(TenantSubscription),
                    tenantId.ToString(),
                    "Subscription is already paid for the current billing period.");
            }

            SubscriptionPlanDefinition planDefinition = await GetAndValidatePlanDefinitionAsync(subscription.Plan, ct);

            SubscriptionPayment payment = BuildPayment(subscription, planDefinition);
            SimulateMockSuccess(payment);
            subscription.RenewBillingPeriod(payment.PaidAt!.Value, payment.Amount);

            List<string> adminEmails = await GetTenantAdminEmailsAsync(tenantId, ct);
            foreach (string email in adminEmails)
            {
                SubscriptionNotification notification = BuildPaymentSucceededNotification(
                    tenantId, planDefinition, payment, subscription, email);
                await notificationRepo.Insert(notification);
            }

            await paymentRepo.Insert(payment);
            await subscriptionRepo.Update(subscription);
            await subscriptionRepo.SaveChangesAsync(ct);

            return payment;
        }

        public async Task InitializeBillingAsync(
            Guid tenantId,
            SubscriptionPlan newPlan,
            CancellationToken ct = default)
        {
            TenantSubscription subscription = await GetSubscriptionAsync(tenantId, ct);
            SubscriptionPlanDefinition planDefinition = await GetAndValidatePlanDefinitionAsync(newPlan, ct);

            subscription.ApplyPlan(planDefinition);

            DateTime now = DateTime.UtcNow;
            subscription.CurrentPeriodStart = now;
            subscription.CurrentPeriodEnd   = now.AddMonths(1);
            subscription.NextPaymentDue     = now.AddMonths(1);
            subscription.GracePeriodEndsAt  = subscription.NextPaymentDue.Value.AddDays(subscription.GracePeriodDays);

            await subscriptionRepo.Update(subscription);
            await subscriptionRepo.SaveChangesAsync(ct);
        }

        private async Task<TenantSubscription> GetAndValidatePaidSubscriptionAsync(
            Guid tenantId,
            CancellationToken ct)
        {
            TenantSubscription subscription = await GetSubscriptionAsync(tenantId, ct);

            if (subscription.Plan == SubscriptionPlan.Free)
            {
                throw new ConflictApiException(nameof(TenantSubscription), tenantId.ToString(), "Free plan does not require payment.");
            }

            return subscription;
        }

        private async Task<TenantSubscription> GetSubscriptionAsync(
            Guid tenantId,
            CancellationToken ct)
        {
            TenantSubscription? subscription = await subscriptionReadRepo.GetFirstBySearch(
                s => s.TenantId == tenantId,
                ct);

            if (subscription is null)
            {
                throw new NotFoundApiException(nameof(TenantSubscription), tenantId.ToString());
            }

            return subscription;
        }

        private async Task<SubscriptionPlanDefinition> GetAndValidatePlanDefinitionAsync(
            SubscriptionPlan plan,
            CancellationToken ct)
        {
            SubscriptionPlanDefinition? planDefinition = await planRepo.GetFirstBySearch(
                p => p.Plan == plan && p.IsActive,
                ct);

            if (planDefinition is null)
            {
                throw new NotFoundApiException(nameof(SubscriptionPlanDefinition), plan.ToString());
            }

            return planDefinition;
        }

        private static SubscriptionPayment BuildPayment(
            TenantSubscription subscription,
            SubscriptionPlanDefinition planDefinition)
        {
            DateTime now = DateTime.UtcNow;
            return new SubscriptionPayment
            {
                TenantSubscriptionId = subscription.Id,
                TenantId             = subscription.TenantId,
                Plan                 = subscription.Plan,
                Amount               = planDefinition.Price,
                Currency             = planDefinition.Currency,
                Status               = SubscriptionPaymentStatus.Pending,
                PeriodStart          = now,
                PeriodEnd            = now.AddMonths(1),
                CreatedAt            = now
            };
        }

        private static void SimulateMockSuccess(SubscriptionPayment payment)
        {
            payment.Status                = SubscriptionPaymentStatus.Succeeded;
            payment.PaidAt                = DateTime.UtcNow;
            payment.ExternalTransactionId = $"MOCK_{Guid.NewGuid():N}";
        }

        private async Task<List<string>> GetTenantAdminEmailsAsync(Guid tenantId, CancellationToken ct)
        {
            List<string> emails = await tenantMemberRepo.SelectAsync(
                m => m.TenantId == tenantId
                     && m.IsActive
                     && m.MemberRole!.Code == RoleCodes.TenantAdmin,
                m => m.User.Email,
                ct);

            return emails;
        }

        private static SubscriptionNotification BuildPaymentSucceededNotification(
            Guid tenantId,
            SubscriptionPlanDefinition planDefinition,
            SubscriptionPayment payment,
            TenantSubscription subscription,
            string recipientEmail)
        {
            return new SubscriptionNotification
            {
                TenantId       = tenantId,
                Type           = SubscriptionNotificationType.PaymentSucceeded,
                RecipientEmail = recipientEmail,
                Subject        = $"Potwierdzenie płatności — {planDefinition.Name}",
                Body           = $"Twoja płatność {payment.Amount} {payment.Currency} została zrealizowana. Subskrypcja aktywna do {subscription.CurrentPeriodEnd:dd.MM.yyyy}.",
                SentAt         = DateTime.UtcNow,
                CreatedAt      = DateTime.UtcNow
            };
        }
    }
}
