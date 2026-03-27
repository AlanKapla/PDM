using Entities.Models.Base;

namespace Entities.Models
{
    public class MessageHistory : BaseEntity
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? ReplyToMessageId { get; set; }

        public bool IsDeleted => DeletedAt.HasValue;

        public Chat Chat { get; set; } = default!;
        public MessageHistory? ReplyToMessage { get; set; }
    }
}
