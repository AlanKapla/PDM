using Entities.Models.Base;

namespace Entities.Models.Chats
{
    public class ChatMember : BaseEntity
    {
        public Guid ChatId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; private set; }
        public bool IsAdmin { get; private set; }

        public Chat Chat { get; private set; } = default!;

        // EF Core constructor.
        private ChatMember() { }

        public ChatMember(Guid chatId, Guid userId, bool isAdmin)
        {
            if (chatId == Guid.Empty)
            {
                throw new ArgumentException("ChatId is required.", nameof(chatId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("UserId is required.", nameof(userId));
            }

            ChatId = chatId;
            UserId = userId;
            IsAdmin = isAdmin;
            JoinedAt = DateTime.UtcNow;
        }

        public void MarkRead(DateTime nowUtc)
        {
            LastReadAt = nowUtc;
        }
    }
}
