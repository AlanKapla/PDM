using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.Subscriptions
{
    /// <summary>
    /// Mock notyfikacji emailowych — zapis do bazy zamiast wysyłki SMTP.
    /// Docelowo zastąpić rzeczywistym providerem email.
    /// </summary>
    public class SubscriptionNotification : BaseEntity
    {
        public Guid TenantId { get; set; }
        public SubscriptionNotificationType Type { get; set; }

        /// <summary>Adres email odbiorcy (snapshot w momencie zapisu).</summary>
        public string RecipientEmail { get; set; } = default!;

        /// <summary>Temat emaila.</summary>
        public string Subject { get; set; } = default!;

        /// <summary>Treść emaila (plain text lub HTML).</summary>
        public string Body { get; set; } = default!;

        /// <summary>NULL = nie wysłano jeszcze (mock: ustaw od razu na CreatedAt).</summary>
        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
