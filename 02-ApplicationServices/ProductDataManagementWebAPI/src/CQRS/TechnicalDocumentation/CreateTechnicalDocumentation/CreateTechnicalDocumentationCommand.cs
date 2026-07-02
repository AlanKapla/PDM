using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using CQRS;
using Microsoft.AspNetCore.Http;

namespace CQRS.TechnicalDocumentation.CreateTechnicalDocumentation;

public sealed record CreateTechnicalDocumentationCommand : IRequestCommand<TechnicalDocumentationCreatedWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required List<IFormFile> Files { get; init; }
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
