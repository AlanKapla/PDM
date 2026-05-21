using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Tenants.GetTenantDetails
{
    public sealed class GetTenantDetailsQueryValidator : AbstractValidator<GetTenantDetailsQuery>
    {
        public GetTenantDetailsQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
        }
    }
}
