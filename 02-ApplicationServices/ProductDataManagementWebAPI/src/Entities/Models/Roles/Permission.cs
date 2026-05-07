using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.Roles
{
    public class Permission : BaseEntity
    {
        public string Code { get; set; } = default!;
        public RoleScope Scope { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsBuiltIn { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
