using FluentValidation;

namespace CQRS.CostEstimates.GetAdditionalFields
{
    public sealed class GetAdditionalFieldsQueryValidator : AbstractValidator<GetAdditionalFieldsQuery>
    {
        public GetAdditionalFieldsQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId jest wymagany");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId jest wymagany");

            RuleFor(x => x.CostEstimateId)
                .NotEmpty()
                .WithMessage("CostEstimateId jest wymagany");
        }
    }
}
