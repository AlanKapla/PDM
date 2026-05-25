using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Tenants.GetAdminTenantDetails;

public sealed class GetAdminTenantDetailsQueryHandler
    : IRequestHandler<GetAdminTenantDetailsQuery, AdminTenantDetailsWeb>
{
    private readonly IReadRepository<Tenant> tenantRepo;

    public GetAdminTenantDetailsQueryHandler(IReadRepository<Tenant> tenantRepo)
    {
        this.tenantRepo = tenantRepo;
    }

    public async Task<AdminTenantDetailsWeb> Handle(
        GetAdminTenantDetailsQuery request,
        CancellationToken cancellationToken)
    {
        Tenant? tenant = await tenantRepo.GetById(
            request.TenantId,
            q => q.Include(t => t.Members),
            q => q.Include(t => t.Projects)
                  .ThenInclude(p => p.Members));

        if (tenant is null)
        {
            throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());
        }

        IEnumerable<AdminTenantProjectItemWeb> projects = tenant.Projects
            .Select(p => new AdminTenantProjectItemWeb(
                p.Id,
                p.Name,
                p.IsActive,
                p.CreatedAt,
                p.Members.Count,
                p.BudgetNet,
                p.BudgetGross));

        return new AdminTenantDetailsWeb(
            tenant.Id,
            tenant.Name,
            tenant.IsActive,
            tenant.CreatedAt,
            tenant.Members.Count,
            projects);
    }
}
