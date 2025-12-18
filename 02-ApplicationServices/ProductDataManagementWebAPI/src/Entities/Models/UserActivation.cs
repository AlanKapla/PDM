using Entities.Models.Base;

namespace Entities.Models
{
    public class UserActivation : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ActivatedAt { get; set; }
        public User User { get; set; } = default!;
        public bool IsActivated => ActivatedAt.HasValue;
    }
}
