using Entities.Models.Base;

namespace Entities.Models.Chats
{
    public class ChatMember : BaseEntity
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }
        public bool IsAdmin { get; set; } = false;

        public Chat Chat { get; set; } = default!;
    }
}
