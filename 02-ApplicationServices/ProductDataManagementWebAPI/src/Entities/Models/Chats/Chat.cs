using Entities.Models.Base;

namespace Entities.Models.Chats
{
    public class Chat : BaseEntity
    {
        public string Name { get; private set; } = default!;
        public bool IsGroupChat { get; private set; }
        public Guid? ProjectId { get; private set; }
        public Guid? TenantId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; private set; }

        public ICollection<ChatMember> Members { get; private set; } = new List<ChatMember>();
        public ICollection<MessageHistory> Messages { get; private set; } = new List<MessageHistory>();

        // EF Core constructor.
        private Chat() { }

        public static Chat CreateDirect(Guid creatorUserId, Guid otherUserId, string displayName)
        {
            if (creatorUserId == Guid.Empty)
            {
                throw new ArgumentException("Creator user id is required.", nameof(creatorUserId));
            }

            if (otherUserId == Guid.Empty)
            {
                throw new ArgumentException("Other user id is required.", nameof(otherUserId));
            }

            if (creatorUserId == otherUserId)
            {
                throw new ArgumentException("Direct chat requires two distinct users.", nameof(otherUserId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Chat display name is required.", nameof(displayName));
            }

            return new Chat
            {
                Name = displayName,
                IsGroupChat = false,
                ProjectId = null,
                TenantId = null,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = creatorUserId
            };
        }

        public static Chat CreateGroup(string name, Guid? tenantId, Guid? projectId, Guid createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Group chat name is required.", nameof(name));
            }

            if (createdByUserId == Guid.Empty)
            {
                throw new ArgumentException("Creator user id is required.", nameof(createdByUserId));
            }

            return new Chat
            {
                Name = name,
                IsGroupChat = true,
                ProjectId = projectId,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };
        }

        public void Rename(string newName)
        {
            if (!IsGroupChat)
            {
                throw new ArgumentException("Cannot rename a direct chat.");
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Chat name cannot be empty.", nameof(newName));
            }

            Name = newName;
        }

        public void ConvertToGroup(Guid projectId, Guid? tenantId)
        {
            if (projectId == Guid.Empty)
            {
                throw new ArgumentException("ProjectId is required.", nameof(projectId));
            }

            IsGroupChat = true;
            ProjectId = projectId;
            TenantId = tenantId;
        }
    }
}
