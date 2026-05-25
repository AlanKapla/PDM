using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Tenants.GetAdminTenants;

public sealed record GetAdminTenantsQuery : IRequestQuery<IEnumerable<AdminTenantListItemWeb>>, ISuperAdminRequest;
