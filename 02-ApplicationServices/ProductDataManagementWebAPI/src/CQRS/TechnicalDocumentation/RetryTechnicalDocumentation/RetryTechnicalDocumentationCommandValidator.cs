using CQRS.Extensions;
using FluentValidation;

namespace CQRS.TechnicalDocumentation.RetryTechnicalDocumentation;

public sealed class RetryTechnicalDocumentationCommandValidator : AbstractValidator<RetryTechnicalDocumentationCommand>
{
    public RetryTechnicalDocumentationCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.DocumentationId).RequiredId();
    }
}
