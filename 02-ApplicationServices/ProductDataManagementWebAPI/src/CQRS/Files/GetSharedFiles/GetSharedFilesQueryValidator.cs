using FluentValidation;

namespace CQRS.Files.GetSharedFiles
{
    public class GetSharedFilesQueryValidator : AbstractValidator<GetSharedFilesQuery>
    {
        public GetSharedFilesQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId jest wymagany");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId jest wymagany");
        }
    }
}
