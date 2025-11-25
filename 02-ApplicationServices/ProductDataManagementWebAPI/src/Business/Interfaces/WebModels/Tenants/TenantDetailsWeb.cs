using Entities.Enums;

namespace Business.Interfaces.WebModels.Tenants
{
    public class TenantDetailsWeb
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public TenantRole Role { get; set; }
        public List<TenantMemberDetailsWeb> Members { get; set; } = new();
        public List<TenantInvitationWeb> Invitations { get; set; } = new();
    }
}
