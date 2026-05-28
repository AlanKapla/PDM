using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using CQRS.Files._Shared;

namespace CQRS.Files.GetProjectFilePackages;

/// <summary>
/// Query to get project file packages based on scope (All, Mine, Shared)
/// </summary>
public sealed record GetProjectFilePackagesQuery : ProjectScopedFilesRequestBase, IRequestQuery<List<ProjectFilePackageWeb>>
{
    public required ResourceScope Scope { get; init; }

    public override string PermissionCode => PermissionCodes.ProjectFiles;

    public ResourceScope? GetResourceScope() => Scope;
}
