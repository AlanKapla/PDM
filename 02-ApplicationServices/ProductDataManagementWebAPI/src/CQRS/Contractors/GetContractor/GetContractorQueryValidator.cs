using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Contractors.GetContractor
{
    public sealed class GetContractorQueryValidator : AbstractValidator<GetContractorQuery>
    {
        public GetContractorQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ContractorId).RequiredId();
        }
    }
}
