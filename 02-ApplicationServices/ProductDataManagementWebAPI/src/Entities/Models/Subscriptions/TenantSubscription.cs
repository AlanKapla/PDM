using Entities.Enums;
using Entities.Models.Base;
using Entities.Models.Tenants;

namespace Entities.Models.Subscriptions
{
    public class TenantSubscription : BaseEntity
    {
        public Guid TenantId { get; set; }

        /// <summary>Aktywny plan tenanta.</summary>
        public SubscriptionPlan Plan { get; set; }

        public SubscriptionStatus Status { get; set; }

        /// <summary>
        /// Snapshot limitu projektów z SubscriptionPlanDefinition w momencie przypisania planu.
        /// Efektywna wartość (po override'ach) pochodzi z SubscriptionLimitsResolver.
        /// </summary>
        public int MaxProjects { get; set; }

        /// <summary>
        /// Snapshot limitu użytkowników z SubscriptionPlanDefinition w momencie przypisania planu.
        /// Efektywna wartość (po override'ach) pochodzi z SubscriptionLimitsResolver.
        /// </summary>
        public int MaxUsers { get; set; }

        public DateTime? TrialEndsAt { get; set; }
        public DateTime CurrentPeriodStart { get; set; }

        /// <summary>NULL dla Free — bezterminowy.</summary>
        public DateTime? CurrentPeriodEnd { get; set; }

        public DateTime? CanceledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ── Full Access ──────────────────────────────────────────────────────────

        /// <summary>
        /// Gdy true — tenant ma nieograniczony dostęp niezależnie od planu i override'ów.
        /// Ustawiany wyłącznie przez admina Brickly. Resolver zwraca Unlimited dla wszystkiego.
        /// </summary>
        public bool IsFullAccess { get; set; }

        /// <summary>ID admina Brickly który przyznał FullAccess. NULL gdy IsFullAccess = false.</summary>
        public Guid? FullAccessGrantedByAdminId { get; set; }

        /// <summary>Kiedy FullAccess został przyznany. NULL gdy IsFullAccess = false.</summary>
        public DateTime? FullAccessGrantedAt { get; set; }

        // ── Billing ────────────────────────────────────────────────────────────────

        /// <summary>Data następnej wymaganej płatności. NULL dla Free.</summary>
        public DateTime? NextPaymentDue { get; set; }

        /// <summary>Data ostatniej udanej płatności. NULL gdy jeszcze nie płacono.</summary>
        public DateTime? LastPaidAt { get; set; }

        /// <summary>Kwota ostatniej płatności.</summary>
        public decimal? LastPaidAmount { get; set; }

        /// <summary>
        /// Ile dni po NextPaymentDue zanim status zmieni się na PastDue.
        /// Domyślnie 7 dni (grace period).
        /// </summary>
        public int GracePeriodDays { get; set; } = 7;

        /// <summary>
        /// Data po której tenant zostaje oznaczony jako PastDue.
        /// Obliczana jako NextPaymentDue + GracePeriodDays.
        /// NULL gdy NextPaymentDue jest NULL.
        /// </summary>
        public DateTime? GracePeriodEndsAt { get; set; }

        // ── Navigation ───────────────────────────────────────────────────────────

        public virtual Tenant Tenant { get; set; } = default!;
        public virtual ICollection<SubscriptionOverride> Overrides { get; set; } = new List<SubscriptionOverride>();
        public virtual ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();

        // ── Factory ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Tworzy domyślną subskrypcję Free dla nowego tenanta.
        /// Snapshot limitów przekazywany z zewnątrz (pobierz z SubscriptionPlanDefinition przed wywołaniem).
        /// </summary>
        public static TenantSubscription CreateDefault(Guid tenantId, SubscriptionPlanDefinition planDefinition)
        {
            DateTime now = DateTime.UtcNow;
            return new TenantSubscription
            {
                Id                 = Guid.NewGuid(),
                TenantId           = tenantId,
                Plan               = planDefinition.Plan,
                Status             = SubscriptionStatus.Active,
                MaxProjects        = planDefinition.MaxProjects,
                MaxUsers           = planDefinition.MaxUsers,
                CurrentPeriodStart = now,
                CreatedAt          = now
            };
        }

        /// <summary>Aktualizuje snapshot limitów po zmianie planu.</summary>
        public void ApplyPlan(SubscriptionPlanDefinition planDefinition)
        {
            Plan        = planDefinition.Plan;
            MaxProjects = planDefinition.MaxProjects;
            MaxUsers    = planDefinition.MaxUsers;
            UpdatedAt   = DateTime.UtcNow;
        }

        /// <summary>Włącza FullAccess. Wywoływać tylko z poziomu admina Brickly.</summary>
        public void GrantFullAccess(Guid adminId)
        {
            IsFullAccess               = true;
            FullAccessGrantedByAdminId = adminId;
            FullAccessGrantedAt        = DateTime.UtcNow;
            UpdatedAt                  = DateTime.UtcNow;
        }

        /// <summary>Odbiera FullAccess — tenant wraca do limitów planu.</summary>
        public void RevokeFullAccess()
        {
            IsFullAccess               = false;
            FullAccessGrantedByAdminId = null;
            FullAccessGrantedAt        = null;
            UpdatedAt                  = DateTime.UtcNow;
        }

        // ── Billing ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Odświeża NextPaymentDue i GracePeriodEndsAt po udanej płatności.
        /// Wywołaj po każdej udanej płatności.
        /// </summary>
        public void RenewBillingPeriod(DateTime paidAt, decimal amount)
        {
            LastPaidAt          = paidAt;
            LastPaidAmount      = amount;
            CurrentPeriodStart  = paidAt;
            CurrentPeriodEnd    = paidAt.AddMonths(1);
            NextPaymentDue      = paidAt.AddMonths(1);
            GracePeriodEndsAt   = NextPaymentDue.Value.AddDays(GracePeriodDays);
            Status              = SubscriptionStatus.Active;
            UpdatedAt           = DateTime.UtcNow;
        }

        /// <summary>
        /// Oznacza subskrypcję jako przeterminowaną po upływie grace period.
        /// Wywołaj z background joba gdy DateTime.UtcNow > GracePeriodEndsAt.
        /// </summary>
        public void MarkAsPastDue()
        {
            Status    = SubscriptionStatus.PastDue;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ustawia GracePeriod gdy NextPaymentDue minęło ale grace period jeszcze trwa.
        /// Wywołaj z background joba gdy DateTime.UtcNow > NextPaymentDue &amp;&amp; &lt; GracePeriodEndsAt.
        /// </summary>
        public void MarkAsGracePeriod()
        {
            Status    = SubscriptionStatus.GracePeriod;
            UpdatedAt = DateTime.UtcNow;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        public bool IsSubscriptionActive =>
            Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing or SubscriptionStatus.GracePeriod;
    }
}
