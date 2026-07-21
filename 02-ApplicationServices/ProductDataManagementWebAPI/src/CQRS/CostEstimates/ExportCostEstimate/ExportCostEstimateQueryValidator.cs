using Business.Interfaces.WebModels.CostEstimates;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.ExportCostEstimate
{
    public sealed class ExportCostEstimateQueryValidator : AbstractValidator<ExportCostEstimateQuery>
    {
        public ExportCostEstimateQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.Format).IsInEnum();
        }
    }
}
