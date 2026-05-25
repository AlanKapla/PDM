using Business.Interfaces.WebModels.Tenants;
using CQRS.Behaviours;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public sealed record ChangeActiveTenantCommand : IRequestCommand<ActiveTenantWeb>, IBypassSubscriptionCheck
    {
        public required Guid TenantId { get; init; }
    }
}
