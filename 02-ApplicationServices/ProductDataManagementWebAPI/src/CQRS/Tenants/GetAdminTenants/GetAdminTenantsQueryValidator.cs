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

            RuleFor(x => x)
                .MustAsync(async (query, ct) =>
                {
                    var adminMemberships = await tenantMemberRepo.GetBySearch(
                        m => m.UserId == currentUser.Id
                             && m.IsActive
                             && m.MemberRole!.Code == RoleCodes.TenantAdmin
                    );
                    return adminMemberships.Any();
                })
                .WithMessage("User must be admin in at least one tenant");
        }
    }
}
