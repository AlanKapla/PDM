using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.UpdateGroupBaseFields
{
    public sealed class UpdateGroupBaseFieldsCommandValidator
        : AbstractValidator<UpdateGroupBaseFieldsCommand>
    {
        public UpdateGroupBaseFieldsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.GroupId).RequiredId();

            RuleFor(x => x.Name)
                .MaximumLength(300)
                .When(x => x.Name is not null);
        }
    }
}
