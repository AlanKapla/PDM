using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using CQRS.Files._Shared;

namespace CQRS.Files.GetPackageFiles;

/// <summary>
/// Query to get files in a specific package based on scope (All, Mine, Shared)
/// Validates user access to the package based on ResourceScope
/// </summary>
public sealed record GetPackageFilesQuery : PackageScopedRequestBase, IRequestQuery<List<ProjectFileWeb>>
{
    public required ResourceScope Scope { get; init; }

    public override string PermissionCode => PermissionCodes.ProjectFiles;

    public ResourceScope? GetResourceScope() => Scope;
}
