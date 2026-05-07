using Entities.Models.Base;

namespace Entities.Models.Chats
{
    public class MessageHistory : DeletableEntity
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public Guid? ReplyToMessageId { get; set; }

        public Chat Chat { get; set; } = default!;
        public MessageHistory? ReplyToMessage { get; set; }
    }
}
