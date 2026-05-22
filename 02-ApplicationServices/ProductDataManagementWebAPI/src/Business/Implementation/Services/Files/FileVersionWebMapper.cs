using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;

namespace Business.Implementation.Services.Files
{
    public sealed class FileVersionWebMapper : IFileVersionWebMapper
    {
        public ProjectFileVersionWeb Map(
            ProjectFileVersionDto versionDto,
            IReadOnlyDictionary<Guid, ProjectMemberUserInfo> userDict,
            FileVersionSasUriInfo? sasUriInfo)
        {
            return new ProjectFileVersionWeb
            {
                Id = versionDto.Id,
                ProjectFileId = versionDto.ProjectFileId,
                VersionNumber = versionDto.VersionNumber,
                ContentType = versionDto.ContentType,
                FileSizeBytes = versionDto.FileSizeBytes,
                CreatedAt = versionDto.CreatedAt,
                CreatedByUserId = versionDto.CreatedByUserId,
                CreatedByUserName = ProjectMemberNameResolver.ResolveUserName(userDict, versionDto.CreatedByUserId),
                SasUrlView = sasUriInfo?.SasUriView ?? string.Empty,
                SasUrlDownload = sasUriInfo?.SasUriDownload ?? string.Empty,
                Comments = new List<ProjectFileVersionCommentWeb>()
            };
        }
    }
}
