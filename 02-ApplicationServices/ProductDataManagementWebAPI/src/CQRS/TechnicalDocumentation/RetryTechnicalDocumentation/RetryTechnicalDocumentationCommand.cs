using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS;
using MediatR;

namespace CQRS.TechnicalDocumentation.RetryTechnicalDocumentation;

public sealed record RetryTechnicalDocumentationCommand(
    Guid TenantId,
    Guid ProjectId,
    Guid DocumentationId)
    : IRequestCommand<Unit>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
