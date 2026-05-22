using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public sealed record ChangeActiveTenantCommand : IRequestCommand<ActiveTenantWeb>
    {
        public required Guid TenantId { get; init; }
    }
}
