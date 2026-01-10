using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;

namespace CQRS.Files.GetVersionComments;

/// <summary>
/// Query to get all comments for a specific file version based on scope (All, Mine, Shared)
/// Validates user access to the file based on ResourceScope
/// </summary>
public sealed record GetVersionCommentsQuery(
    Guid TenantId,
    Guid ProjectId,
    Guid FileId,
    Guid VersionId,
    ResourceScope Scope
) : IRequestQuery<List<ProjectFileVersionCommentWeb>>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectView;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    
    public ResourceScope? GetResourceScope() => Scope;
}
