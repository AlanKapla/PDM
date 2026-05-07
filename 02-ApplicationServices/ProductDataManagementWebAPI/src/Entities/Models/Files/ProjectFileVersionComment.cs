using Entities.Models.Base;
using Entities.Models.Users;

namespace Entities.Models.Files
{
    public class ProjectFileVersionComment : DeletableEntity
    {
        public Guid ProjectFileVersionId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }

        public ProjectFileVersion ProjectFileVersion { get; set; } = default!;
        public User User { get; set; } = default!;
    }
}
