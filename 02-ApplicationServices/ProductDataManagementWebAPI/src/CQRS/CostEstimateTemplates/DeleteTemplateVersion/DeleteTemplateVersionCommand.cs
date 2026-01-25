using MediatR;

namespace CQRS.CostEstimateTemplates.DeleteTemplateVersion
{
    /// <summary>
    /// Command do usunięcia wersji szablonu kosztorysu
    /// </summary>
    public record DeleteTemplateVersionCommand(
        Guid TemplateId,
        Guid VersionId
    ) : IRequestCommand<Unit>;
}
