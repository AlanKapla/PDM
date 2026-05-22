using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Tenants.GetTenantMembers
{
    public sealed class GetTenantMembersQueryValidator : AbstractValidator<GetTenantMembersQuery>
    {
        public GetTenantMembersQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
        }
    }
}
