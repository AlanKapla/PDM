using Business.Implementation.Services.Files;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using FluentAssertions;

namespace Business.Tests.Services.Files;

public class FileVersionWebMapperTests
{
    private readonly FileVersionWebMapper _sut = new FileVersionWebMapper();

    private static ProjectFileVersionDto BuildDto() =>
        new ProjectFileVersionDto
        {
            Id = Guid.NewGuid(),
            ProjectFileId = Guid.NewGuid(),
            VersionNumber = 3,
            CreatedByUserId = Guid.NewGuid(),
            BlobFileName = "report.pdf",
            BlobPath = "blobs/report.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 12_345,
            CreatedAt = new DateTime(2024, 5, 20, 10, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        };

    private static FileVersionSasUriInfo BuildSasInfo(Guid versionId) =>
        new FileVersionSasUriInfo
        {
            VersionId = versionId,
            SasUriView = "https://blob.example.com/view?sas=abc",
            SasUriDownload = "https://blob.example.com/download?sas=xyz",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(55)
        };

    // ─── Field mapping ────────────────────────────────────────────────────────

    [Fact]
    public void Map_AllFields_MappedCorrectly()
    {
        ProjectFileVersionDto dto = BuildDto();
        ProjectMemberUserInfo user = new ProjectMemberUserInfo
        {
            UserId = dto.CreatedByUserId,
            FirstName = "Jan",
            LastName = "Kowalski"
        };
        Dictionary<Guid, ProjectMemberUserInfo> userDict = new Dictionary<Guid, ProjectMemberUserInfo>
        {
            { user.UserId, user }
        };

        FileVersionSasUriInfo sasInfo = BuildSasInfo(dto.Id);

        ProjectFileVersionWeb result = _sut.Map(dto, userDict, sasInfo);

        result.Id.Should().Be(dto.Id);
        result.ProjectFileId.Should().Be(dto.ProjectFileId);
        result.VersionNumber.Should().Be(dto.VersionNumber);
        result.ContentType.Should().Be(dto.ContentType);
        result.FileSizeBytes.Should().Be(dto.FileSizeBytes);
        result.CreatedAt.Should().Be(dto.CreatedAt);
        result.CreatedByUserId.Should().Be(dto.CreatedByUserId);
        result.CreatedByUserName.Should().Be("Jan Kowalski");
        result.SasUrlView.Should().Be(sasInfo.SasUriView);
        result.SasUrlDownload.Should().Be(sasInfo.SasUriDownload);
    }

    [Fact]
    public void Map_UnknownUser_ReturnsEmptyUserName()
    {
        ProjectFileVersionDto dto = BuildDto();
        Dictionary<Guid, ProjectMemberUserInfo> emptyDict =
            new Dictionary<Guid, ProjectMemberUserInfo>();

        ProjectFileVersionWeb result = _sut.Map(dto, emptyDict, null);

        result.CreatedByUserName.Should().BeEmpty();
    }

    [Fact]
    public void Map_NullSasUriInfo_ReturnEmptySasUrls()
    {
        ProjectFileVersionDto dto = BuildDto();
        Dictionary<Guid, ProjectMemberUserInfo> emptyDict =
            new Dictionary<Guid, ProjectMemberUserInfo>();

        ProjectFileVersionWeb result = _sut.Map(dto, emptyDict, null);

        result.SasUrlView.Should().BeEmpty();
        result.SasUrlDownload.Should().BeEmpty();
    }

    [Fact]
    public void Map_UserWithOnlyFirstName_ReturnsFirstName()
    {
        ProjectFileVersionDto dto = BuildDto();
        ProjectMemberUserInfo user = new ProjectMemberUserInfo
        {
            UserId = dto.CreatedByUserId,
            FirstName = "Anna",
            LastName = string.Empty
        };
        Dictionary<Guid, ProjectMemberUserInfo> userDict = new Dictionary<Guid, ProjectMemberUserInfo>
        {
            { user.UserId, user }
        };

        ProjectFileVersionWeb result = _sut.Map(dto, userDict, null);

        result.CreatedByUserName.Should().Be("Anna");
    }

    [Fact]
    public void Map_CommentsCollectionIsEmpty()
    {
        ProjectFileVersionDto dto = BuildDto();
        ProjectFileVersionWeb result = _sut.Map(dto,
            new Dictionary<Guid, ProjectMemberUserInfo>(), null);

        result.Comments.Should().BeEmpty();
    }
}
