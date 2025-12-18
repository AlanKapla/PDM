using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;

namespace CQRS.Tenants.ToggleTenantStatus;

/// <summary>
/// Walidator dla ToggleTenantStatusCommand
/// </summary>
public class ToggleTenantStatusCommandValidator : AbstractValidator<ToggleTenantStatusCommand>
{
    private readonly IReadRepository<Tenant> tenantRepo;

    public ToggleTenantStatusCommandValidator(IReadRepository<Tenant> tenantRepo)
    {
        this.tenantRepo = tenantRepo;

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.TenantId)
            .MustAsync(TenantExists)
            .WithMessage("Tenant not found");
    }

    private async Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await tenantRepo.GetById(tenantId);
        return tenant != null;
    }
}
