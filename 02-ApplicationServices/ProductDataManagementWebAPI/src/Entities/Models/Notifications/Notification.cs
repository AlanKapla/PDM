using Entities.Models.Base;
using Entities.Models.Projects;
using Entities.Models.Tenants;

namespace Entities.Models.Notifications
{
    public class Notification : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public string? MetadataJson { get; set; }

        public Tenant Tenant { get; set; } = default!;
        public Project? Project { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}
