using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;

namespace CQRS.Files.GetPackageFiles;

/// <summary>
/// Query to get files in a specific package based on scope (All, Mine, Shared)
/// Validates user access to the package based on ResourceScope
/// </summary>
public sealed record GetPackageFilesQuery(
    Guid TenantId,
    Guid ProjectId,
    Guid PackageId,
    ResourceScope Scope
) : IRequestQuery<List<ProjectFileWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectView;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    
    public ResourceScope? GetResourceScope() => Scope;
}
