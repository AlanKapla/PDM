using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;

namespace CQRS.Files.GetProjectFilePackages;

/// <summary>
/// Query to get project file packages based on scope (All, Mine, Shared)
/// </summary>
public sealed record GetProjectFilePackagesQuery(
    Guid TenantId,
    Guid ProjectId,
    ResourceScope Scope
) : IRequestQuery<List<ProjectFilePackageWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectView;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    
    public ResourceScope? GetResourceScope() => Scope;
}
