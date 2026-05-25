using Entities.Models.Base;

namespace Entities.Models.Subscriptions
{
    public class SubscriptionOverride : BaseEntity
    {
        public Guid TenantSubscriptionId { get; set; }

        /// <summary>
        /// Klucz override'a. Konwencja:
        /// "MaxProjects" | "MaxUsers" | "Feature:{NazwaFunkcji}"
        /// </summary>
        public string Key { get; set; } = default!;

        /// <summary>Wartość jako string. Int jako liczba, bool jako "true"/"false".</summary>
        public string Value { get; set; } = default!;

        /// <summary>Obowiązkowe uzasadnienie.</summary>
        public string Reason { get; set; } = default!;

        /// <summary>ID admina Brickly który ustawił override.</summary>
        public Guid SetByAdminId { get; set; }

        /// <summary>NULL = bezterminowo.</summary>
        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>False = dezaktywowany ręcznie. Nigdy nie usuwaj rekordów — audit log.</summary>
        public bool IsActive { get; set; } = true;

        public virtual TenantSubscription TenantSubscription { get; set; } = default!;

        public bool IsValid() =>
            IsActive && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

        public static class Keys
        {
            public const string MaxProjects = "MaxProjects";
            public const string MaxUsers    = "MaxUsers";

            public static string Feature(string name) => $"Feature:{name}";
        }
    }
}
