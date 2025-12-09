using Entities.Models.Base;

namespace Entities.Models
{
    public class Chat : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public bool IsGroupChat { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public Tenant Tenant { get; set; } = default!;
        public Project Project { get; set; } = default!;
        public TenantMember CreatedBy { get; set; } = default!;
        public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
        public ICollection<MessageHistory> Messages { get; set; } = new List<MessageHistory>();
    }
}
