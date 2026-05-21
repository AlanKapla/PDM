using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.DeleteProjectFile
{
    /// <summary>
    /// Command to delete a project file
    /// </summary>
    public sealed record DeleteProjectFileCommand : FileScopedRequestBase, IRequestCommand<Unit>
    {
        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
