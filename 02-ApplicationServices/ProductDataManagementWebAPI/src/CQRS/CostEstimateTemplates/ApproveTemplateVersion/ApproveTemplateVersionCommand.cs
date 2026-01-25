using MediatR;

namespace CQRS.CostEstimateTemplates.ApproveTemplateVersion
{
    /// <summary>
    /// Command do zatwierdzenia wersji szablonu kosztorysu
    /// </summary>
    public record ApproveTemplateVersionCommand(
        Guid TemplateId,
        Guid VersionId
    ) : IRequestCommand<Unit>;
}
