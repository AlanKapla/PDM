using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;

namespace CQRS.Files.GetFileVersions;

/// <summary>
/// Query to get all versions of a specific file based on scope (All, Mine, Shared)
/// Validates user access to the file based on ResourceScope
/// </summary>
public sealed record GetFileVersionsQuery(
    Guid TenantId,
    Guid ProjectId,
    Guid FileId,
    ResourceScope Scope
) : IRequestQuery<List<ProjectFileVersionWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectView;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    
    public ResourceScope? GetResourceScope() => Scope;
}
