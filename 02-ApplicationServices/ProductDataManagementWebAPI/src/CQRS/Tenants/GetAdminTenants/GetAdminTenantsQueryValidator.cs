using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetAdminTenants
{
    public class GetAdminTenantsQueryValidator : AbstractValidator<GetAdminTenantsQuery>
    {
        public GetAdminTenantsQueryValidator(
            ICurrentUser currentUser,
            IRepository<TenantMember> tenantMemberRepo)
        {
            RuleFor(x => currentUser.IsAuthenticated)
                .Equal(true)
                .WithMessage("User must be authenticated");

            RuleFor(x => currentUser.Id)
                .NotEqual(Guid.Empty)
                .WithMessage("Invalid user");
        }
    }
}
