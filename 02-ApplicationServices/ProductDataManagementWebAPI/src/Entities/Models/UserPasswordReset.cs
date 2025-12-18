using Entities.Models.Base;

namespace Entities.Models
{
    public class UserPasswordReset : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = default!; // secure random token
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
        public bool IsUsed => UsedAt.HasValue; // reverted to computed
        public User User { get; set; } = default!; // navigation
    }
}
