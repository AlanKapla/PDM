using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.Subscriptions
{
    public class SubscriptionPlanDefinition : BaseEntity
    {
        /// <summary>Typ planu — unikalny klucz biznesowy.</summary>
        public SubscriptionPlan Plan { get; set; }

        /// <summary>Wyświetlana nazwa planu (np. "Free", "Standard").</summary>
        public string Name { get; set; } = default!;

        /// <summary>Maksymalna liczba projektów. -1 = bez limitu.</summary>
        public int MaxProjects { get; set; }

        /// <summary>Maksymalna liczba użytkowników. -1 = bez limitu.</summary>
        public int MaxUsers { get; set; }

        /// <summary>Cena miesięczna netto. 0 dla Free.</summary>
        public decimal Price { get; set; }

        /// <summary>Waluta ceny (np. "PLN", "EUR").</summary>
        public string Currency { get; set; } = "PLN";

        /// <summary>Czy plan jest widoczny dla użytkowników (można ukryć przestarzałe plany).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
