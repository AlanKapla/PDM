using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using CQRS;

namespace CQRS.TechnicalDocumentation.GetTechnicalDocumentationList;

public sealed record GetTechnicalDocumentationListQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<List<TechnicalDocumentationListItemWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
