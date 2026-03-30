using Entities.Models.Base;

namespace Entities.Models
{
    public class Chat : BaseEntity
    {
        public string Name { get; set; } = default!;
        public bool IsGroupChat { get; set; } = false;
        public Guid? ProjectId { get; set; }
        public Guid? TenantId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
        public ICollection<MessageHistory> Messages { get; set; } = new List<MessageHistory>();
    }
}
