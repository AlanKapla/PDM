namespace Business.Interfaces.WebModels.Tenants
{
    public class TenantMembersWeb
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public List<TenantMemberDetailsWeb> Members { get; set; } = new();
    }
}
