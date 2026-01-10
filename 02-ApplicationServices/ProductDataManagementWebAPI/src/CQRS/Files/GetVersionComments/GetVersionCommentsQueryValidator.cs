using FluentValidation;

namespace CQRS.Files.GetVersionComments;

public class GetVersionCommentsQueryValidator : AbstractValidator<GetVersionCommentsQuery>
{
    public GetVersionCommentsQueryValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("FileId is required");
            
        RuleFor(x => x.VersionId)
            .NotEmpty()
            .WithMessage("VersionId is required");

        RuleFor(x =>x.Scope)
            .IsInEnum()
            .WithMessage("Scope is invalid");
    }
}
