using Entities.Models.Base;

namespace Entities.Models.Chats
{
    public class MessageHistory : DeletableEntity
    {
        public const int MaxContentLength = 4000;

        public Guid ChatId { get; private set; }
        public Guid UserId { get; private set; }
        public string Content { get; private set; } = default!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; private set; }
        public Guid? ReplyToMessageId { get; private set; }

        public Chat Chat { get; private set; } = default!;
        public MessageHistory? ReplyToMessage { get; private set; }

        // EF Core constructor.
        private MessageHistory() { }

        public static MessageHistory Create(Guid chatId, Guid authorId, string content, Guid? replyToId)
        {
            if (chatId == Guid.Empty)
            {
                throw new ArgumentException("ChatId is required.", nameof(chatId));
            }

            if (authorId == Guid.Empty)
            {
                throw new ArgumentException("AuthorId is required.", nameof(authorId));
            }

            ValidateContent(content);

            return new MessageHistory
            {
                ChatId = chatId,
                UserId = authorId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = replyToId
            };
        }

        public void Edit(string newContent, DateTime nowUtc)
        {
            ValidateContent(newContent);
            Content = newContent;
            EditedAt = nowUtc;
        }

        public void SoftDelete(DateTime nowUtc)
        {
            IsDeleted = true;
            DeletedAt = nowUtc;
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content cannot be empty.", nameof(content));
            }

            if (content.Length > MaxContentLength)
            {
                throw new ArgumentException(
                    $"Message content cannot exceed {MaxContentLength} characters.",
                    nameof(content));
            }
        }
    }
}
