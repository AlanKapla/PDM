using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Tenants.GetAdminTenantDetails;

public sealed record GetAdminTenantDetailsQuery(Guid TenantId)
    : IRequestQuery<AdminTenantDetailsWeb>, ISuperAdminRequest;
