using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Contractors.DeleteContractor
{
    public sealed class DeleteContractorCommandValidator : AbstractValidator<DeleteContractorCommand>
    {
        public DeleteContractorCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.Id).RequiredId();
        }
    }
}
