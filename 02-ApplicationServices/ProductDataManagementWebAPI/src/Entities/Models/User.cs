using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SystemRole SystemRole { get; set; } = SystemRole.User;
        public Guid? ActiveTenantId { get; set; }


        public ICollection<TenantMember> TenantMemberships { get; set; } = new List<TenantMember>();
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    }
}
