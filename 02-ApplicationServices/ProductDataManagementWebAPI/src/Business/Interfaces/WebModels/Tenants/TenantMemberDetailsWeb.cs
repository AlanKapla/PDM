using Entities.Enums;

namespace Business.Interfaces.WebModels.Tenants
{
    public class TenantMemberDetailsWeb
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public TenantRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
