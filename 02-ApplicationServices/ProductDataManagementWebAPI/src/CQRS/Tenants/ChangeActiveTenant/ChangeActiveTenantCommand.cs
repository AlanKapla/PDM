using Business.Interfaces.WebModels.Tenants;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public sealed record ChangeActiveTenantCommand(Guid TenantId) : IRequestCommand<ActiveTenantWeb>;
}
