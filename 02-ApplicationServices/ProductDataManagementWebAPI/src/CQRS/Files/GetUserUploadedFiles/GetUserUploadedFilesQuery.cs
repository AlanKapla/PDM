using Business.Interfaces.DTO;
using Business.Interfaces.WebModels.Files;
using CQRS.Files.UploadProjectFiles;

namespace CQRS.Files.GetUserUploadedFiles
{
    /// <summary>
    /// Query do pobierania plików przesłanych przez użytkownika w projekcie
    /// </summary>
    public record GetUserUploadedFilesQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<ProjectFileWeb>>;
}
