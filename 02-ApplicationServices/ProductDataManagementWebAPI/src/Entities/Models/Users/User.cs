using Entities.Enums;
using Entities.Models;
using Entities.Models.Base;
using Entities.Models.Tenants;

namespace Entities.Models.Users
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string AzureAdB2CObjectId { get; set; } = default!;
        public bool IsActive { get; set; } = false;
        public DateTime? WelcomeEmailSentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SystemRole SystemRole { get; set; } = SystemRole.User;

        // Kontaktowe
        public string? PhoneNumber { get; set; }

        // Firmowe
        public string? CompanyName { get; set; }
        public string? TaxId { get; set; }

        // Adresowe
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        public ICollection<TenantMember> TenantMemberships { get; set; } = new List<TenantMember>();
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public ICollection<UserProfileBase> Profiles { get; set; } = new List<UserProfileBase>();
    }
}
