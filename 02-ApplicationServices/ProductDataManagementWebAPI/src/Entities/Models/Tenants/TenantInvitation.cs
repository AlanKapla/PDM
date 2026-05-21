using Entities.Models.Base;
using Entities.Models.Users;

namespace Entities.Models.Tenants
{
    public class TenantInvitation : BaseEntity
    {
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = default!;
        public string Email { get; set; } = string.Empty; // email adresata zaproszenia
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid InvitedByUserId { get; set; }
        public User InvitedByUser { get; set; } = default!; // nawigacja do użytkownika który wysłał zaproszenie
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public bool IsActive { get; set; }
        public InvitationStatus Status { get; set; }
    }

    public enum InvitationStatus
    {
        Pending = 0,
        Accepted = 1,
        Revoked = 2
    }
}
