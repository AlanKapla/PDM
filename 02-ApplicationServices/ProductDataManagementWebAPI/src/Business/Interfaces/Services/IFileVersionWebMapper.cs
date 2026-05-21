using Business.Interfaces.DTO;
using Business.Interfaces.WebModels.Files;

namespace Business.Interfaces.Services
{
    public interface IFileVersionWebMapper
    {
        ProjectFileVersionWeb Map(
            ProjectFileVersionDto versionDto,
            IReadOnlyDictionary<Guid, ProjectMemberUserInfo> userDict,
            FileVersionSasUriInfo? sasUriInfo);
    }
}
