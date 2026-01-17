using Entities.Enums;

namespace Business.Interfaces.WebModels.Tenants
{
    /// <summary>
    /// Tenant details with role code instead of enum
    /// </summary>
    public class TenantDetailsWeb
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string RoleCode { get; set; } = string.Empty;  // Changed from TenantRole enum
        public List<TenantMemberWeb> Members { get; set; } = new();
        public List<TenantInvitationWeb> Invitations { get; set; } = new();
    }
}
