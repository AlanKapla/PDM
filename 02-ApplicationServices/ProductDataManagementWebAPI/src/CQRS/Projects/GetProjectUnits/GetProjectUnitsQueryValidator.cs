using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetProjectUnits
{
    public sealed class GetProjectUnitsQueryValidator : AbstractValidator<GetProjectUnitsQuery>
    {
        public GetProjectUnitsQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
