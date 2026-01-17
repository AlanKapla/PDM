using Entities.Models.Base;

namespace Entities.Models;

public abstract class UserProfileBase : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}

public class TenantPreferencesProfile : UserProfileBase
{
    public Guid? ActiveTenantId { get; set; }
}

public class PermissionsVersionProfile : UserProfileBase
{
    public int Version { get; set; } = 1;
}
