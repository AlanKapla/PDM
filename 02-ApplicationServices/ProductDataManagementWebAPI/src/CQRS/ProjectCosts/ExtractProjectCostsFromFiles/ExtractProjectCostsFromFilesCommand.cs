using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.ProjectCosts.ExtractProjectCostsFromFiles;

/// <summary>
/// Command to extract project cost data from uploaded files using AI
/// Supports JPG and PDF files, max 50MB total
/// </summary>
public sealed record ExtractProjectCostsFromFilesCommand : IRequestCommand<ExtractProjectCostsFromFilesResponseWeb>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public List<IFormFile> Files { get; init; } = new();

    public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
