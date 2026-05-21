namespace Business.Interfaces.WebModels.Tenants
{
    public sealed record ActiveTenantWeb
    {
        public Guid? ActiveTenantId { get; init; }
    }
}
