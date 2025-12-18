using Entities.Models.Base;

namespace Entities.Models
{
    public class MessageHistory : BaseEntity
    {
        public Guid ChatId { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Chat Chat { get; set; } = default!;
        public TenantMember User { get; set; } = default!;
    }
}
