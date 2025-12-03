using MediatR;

namespace CQRS.Files.DeleteProjectFile
{
    /// <summary>
    /// Command to delete a project file
    /// </summary>
    public record DeleteProjectFileCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid FileId
    ) : IRequestCommand<Unit>;
}
