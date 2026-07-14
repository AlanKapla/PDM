using Entities.Enums;

namespace Entities.Models.Tenants;

public class TenantInvitationModulePermission
{
    public Guid InvitationId { get; set; }
    public ProjectModule Module { get; set; }

    public TenantInvitation Invitation { get; set; } = default!;
}
