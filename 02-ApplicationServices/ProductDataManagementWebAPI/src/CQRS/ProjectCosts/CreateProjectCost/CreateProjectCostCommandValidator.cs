using CQRS.Extensions;
using CQRS.ProjectCosts.Shared;
using FluentValidation;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    public sealed class CreateProjectCostCommandValidator : AbstractValidator<CreateProjectCostCommand>
    {
        public CreateProjectCostCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            this.ApplyCostNameRules(x => x.Name);
            this.ApplyCostDateRules(x => x.Date);
            this.ApplyCostFinancialRules(x => x.Net, x => x.Gross);
            this.ApplyDocumentRules(x => x.Document, "Document");

            RuleFor(x => x.ContractorId)
                .NotEqual(Guid.Empty)
                .When(x => x.ContractorId.HasValue);

            RuleFor(x => x.Number)
                .MaximumLength(100)
                .WithMessage("Number cannot exceed 100 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Number));

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));
        }
    }
}
