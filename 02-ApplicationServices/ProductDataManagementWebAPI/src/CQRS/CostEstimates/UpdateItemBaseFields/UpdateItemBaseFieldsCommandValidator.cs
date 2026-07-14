using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.UpdateItemBaseFields
{
    public sealed class UpdateItemBaseFieldsCommandValidator
        : AbstractValidator<UpdateItemBaseFieldsCommand>
    {
        public UpdateItemBaseFieldsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();

            RuleFor(x => x.Name)
                .MaximumLength(300)
                .When(x => x.Name is not null);

            RuleFor(x => x.Unit)
                .MaximumLength(50)
                .When(x => x.Unit is not null);

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Quantity.HasValue);

            RuleFor(x => x.UnitPriceNet)
                .GreaterThanOrEqualTo(0)
                .When(x => x.UnitPriceNet.HasValue);

            RuleFor(x => x.VatRate)
                .InclusiveBetween(0m, 1m)
                .When(x => x.VatRate.HasValue);

            RuleFor(x => x.NetValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.NetValue.HasValue);

            RuleFor(x => x.GrossValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.GrossValue.HasValue);

            RuleFor(x => x.VatValue)
                .GreaterThanOrEqualTo(0)
                .When(x => x.VatValue.HasValue);

            RuleFor(x => x.UnitPriceGross)
                .GreaterThanOrEqualTo(0)
                .When(x => x.UnitPriceGross.HasValue);
        }
    }
}
