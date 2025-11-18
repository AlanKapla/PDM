namespace Business.Interfaces.WebModels.Tenants
{
    public class TenantDetailsWeb
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
