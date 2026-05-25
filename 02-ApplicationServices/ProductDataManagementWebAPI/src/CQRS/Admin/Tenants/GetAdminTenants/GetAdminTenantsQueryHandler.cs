using Business.Interfaces.WebModels.Admin;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Tenants.GetAdminTenants;

public sealed class GetAdminTenantsQueryHandler
    : IRequestHandler<GetAdminTenantsQuery, IEnumerable<AdminTenantListItemWeb>>
{
    private readonly IReadRepository<Tenant> tenantRepo;

    public GetAdminTenantsQueryHandler(IReadRepository<Tenant> tenantRepo)
    {
        this.tenantRepo = tenantRepo;
    }

    public async Task<IEnumerable<AdminTenantListItemWeb>> Handle(
        GetAdminTenantsQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<Tenant> tenants = await tenantRepo.GetBySearch(
            t => true,
            q => q.Include(t => t.Members),
            q => q.Include(t => t.Projects));

        return tenants.Select(t => new AdminTenantListItemWeb(
            t.Id,
            t.Name,
            t.IsActive,
            t.CreatedAt,
            t.Members.Count,
            t.Projects.Count
        ));
    }
}
