using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using CQRS.Files._Shared;

namespace CQRS.Files.GetVersionComments;

/// <summary>
/// Query to get all comments for a specific file version based on scope (All, Mine, Shared)
/// Validates user access to the file based on ResourceScope
/// </summary>
public sealed record GetVersionCommentsQuery : FileScopedRequestBase, IRequestQuery<List<ProjectFileVersionCommentWeb>>
{
    public required Guid VersionId { get; init; }
    public required ResourceScope Scope { get; init; }

    public override string PermissionCode => PermissionCodes.ProjectView;

    public ResourceScope? GetResourceScope() => Scope;
}
