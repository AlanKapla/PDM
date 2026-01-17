using FluentValidation;

namespace CQRS.Files.GetFileVersions;

public class GetFileVersionsQueryValidator : AbstractValidator<GetFileVersionsQuery>
{
    public GetFileVersionsQueryValidator()
    {           
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("FileId is required");

        RuleFor(x => x.Scope)
            .IsInEnum()
            .WithMessage("Scope is invalid");
    }
}
