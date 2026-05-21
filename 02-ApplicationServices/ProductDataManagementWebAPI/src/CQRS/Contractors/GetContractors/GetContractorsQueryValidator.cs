using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Contractors.GetContractors
{
    public sealed class GetContractorsQueryValidator : AbstractValidator<GetContractorsQuery>
    {
        public GetContractorsQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();

            RuleFor(x => x.Search)
                .MaximumLength(200)
                .When(x => x.Search is not null);
        }
    }
}