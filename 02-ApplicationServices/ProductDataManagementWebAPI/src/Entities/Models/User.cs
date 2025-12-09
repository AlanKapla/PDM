using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = default!;
        public string? PasswordHash { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SystemRole SystemRole { get; set; } = SystemRole.User;
        
        public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
        public string? ExternalId { get; set; }
        
        public bool HasLocalAuth => !string.IsNullOrEmpty(PasswordHash);
        public bool HasGoogleAuth => !string.IsNullOrEmpty(ExternalId);
        public bool IsHybridAuth => HasLocalAuth && HasGoogleAuth;

        public ICollection<TenantMember> TenantMemberships { get; set; } = new List<TenantMember>();
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public ICollection<UserPasswordReset> PasswordResets { get; set; } = new List<UserPasswordReset>();
        public ICollection<UserActivation> Activations { get; set; } = new List<UserActivation>();
        public ICollection<UserProfileBase> Profiles { get; set; } = new List<UserProfileBase>();
    }

    public enum AuthProvider
    {
        Local = 0,
        Google = 1
    }
}
