using Entities.Models.Base;
using System;

namespace Entities.Models
{
    public class Notification : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool Readed { get; set; } = false;
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
