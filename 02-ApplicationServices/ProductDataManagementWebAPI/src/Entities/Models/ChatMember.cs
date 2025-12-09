using Entities.Models.Base;

namespace Entities.Models
{
    public class ChatMember : BaseEntity
    {
        public Guid ChatId { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }

        public Chat Chat { get; set; } = default!;
        public TenantMember User { get; set; } = default!;
    }
}
