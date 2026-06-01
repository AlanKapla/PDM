using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.CreateDirectory
{
    public sealed record CreateDirectoryCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
    {
        public required string DirectoryName { get; init; }
        public Guid? ParentId { get; init; }
        public override string PermissionCode => PermissionCodes.ProjectFiles;
    }
}
