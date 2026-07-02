using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using CQRS;

namespace CQRS.TechnicalDocumentation.GetTechnicalDocumentationDetails;

public sealed record GetTechnicalDocumentationDetailsQuery(
    Guid TenantId,
    Guid ProjectId,
    Guid DocumentationId)
    : IRequestQuery<TechnicalDocumentationDetailsWeb>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
