using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetTenantDetails
{
    public class GetTenantDetailsQueryValidator : AbstractValidator<GetTenantDetailsQuery>
    {
        public GetTenantDetailsQueryValidator(
            ICurrentUser currentUser,
            IRepository<TenantMember> tenantMemberRepo)
        {
            RuleFor(x => x.TenantId)
                .NotEqual(Guid.Empty)
                .WithMessage("Invalid tenant ID");
        }
    }
}
