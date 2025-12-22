using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string AzureAdB2CObjectId { get; set; } = default!;
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SystemRole SystemRole { get; set; } = SystemRole.User;

        public ICollection<TenantMember> TenantMemberships { get; set; } = new List<TenantMember>();
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public ICollection<UserProfileBase> Profiles { get; set; } = new List<UserProfileBase>();
    }
}
