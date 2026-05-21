using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using CQRS.Files._Shared;

namespace CQRS.Files.GetFileVersions;

/// <summary>
/// Query to get all versions of a specific file based on scope (All, Mine, Shared)
/// Validates user access to the file based on ResourceScope
/// </summary>
public sealed record GetFileVersionsQuery : FileScopedRequestBase, IRequestQuery<List<ProjectFileVersionWeb>>
{
    public required ResourceScope Scope { get; init; }

    public override string PermissionCode => PermissionCodes.ProjectView;

    public ResourceScope? GetResourceScope() => Scope;
}
