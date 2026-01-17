using FluentValidation;

namespace CQRS.Projects.GetProjectsDictionary
{
    public class GetProjectsDictionaryQueryValidator : AbstractValidator<GetProjectsDictionaryQuery>
    {
        public GetProjectsDictionaryQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");
        }
    }
}
