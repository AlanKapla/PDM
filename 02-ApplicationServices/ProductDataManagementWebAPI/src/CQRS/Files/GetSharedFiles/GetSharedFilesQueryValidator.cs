using FluentValidation;

namespace CQRS.Files.GetSharedFiles
{
    public class GetSharedFilesQueryValidator : AbstractValidator<GetSharedFilesQuery>
    {
        public GetSharedFilesQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");
        }
    }
}
