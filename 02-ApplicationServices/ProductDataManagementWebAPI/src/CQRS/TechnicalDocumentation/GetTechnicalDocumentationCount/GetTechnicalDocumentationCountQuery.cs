using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS;

namespace CQRS.TechnicalDocumentation.GetTechnicalDocumentationCount;

public sealed record GetTechnicalDocumentationCountQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<int>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
