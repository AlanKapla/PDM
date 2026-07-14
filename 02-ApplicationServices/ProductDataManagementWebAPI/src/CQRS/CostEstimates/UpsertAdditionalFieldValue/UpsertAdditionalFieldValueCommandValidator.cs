using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.UpsertAdditionalFieldValue
{
    public sealed class UpsertAdditionalFieldValueCommandValidator
        : AbstractValidator<UpsertAdditionalFieldValueCommand>
    {
        public UpsertAdditionalFieldValueCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.AdditionalFieldId).NotEmpty();

            // Jedno z GroupId/ItemId musi być ustawione, ale nie oba
            RuleFor(x => x)
                .Must(x => (x.GroupId.HasValue || x.ItemId.HasValue)
                    && !(x.GroupId.HasValue && x.ItemId.HasValue))
                .WithMessage("Musisz podać GroupId lub ItemId, ale nie oba");
        }
    }
}
