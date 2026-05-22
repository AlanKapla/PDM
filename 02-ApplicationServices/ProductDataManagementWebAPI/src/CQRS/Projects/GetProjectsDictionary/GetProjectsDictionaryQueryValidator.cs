using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetProjectsDictionary
{
    public sealed class GetProjectsDictionaryQueryValidator : AbstractValidator<GetProjectsDictionaryQuery>
    {
        public GetProjectsDictionaryQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
        }
    }
}
