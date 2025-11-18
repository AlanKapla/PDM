using Entities.Models.Base;

namespace Entities.Models
{
    public class UserSession : BaseEntity
    {
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = default!;
        public bool IsRevoked { get; set; }

        public virtual User User { get; set; } = default!;
    }
}
