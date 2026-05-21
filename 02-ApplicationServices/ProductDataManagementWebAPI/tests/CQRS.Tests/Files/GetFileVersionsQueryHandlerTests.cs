using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using CQRS.Files.GetFileVersions;
using Entities.Models.Files;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.Files;

public sealed class GetFileVersionsQueryHandlerTests
{
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IFileVersionWebMapper> _mapperMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetFileVersionsQueryHandler _handler;

    public GetFileVersionsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _userServiceMock
            .Setup(s => s.GetProjectMembersByIdsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ProjectMemberUserInfo>());

        _projectFilesServiceMock
            .Setup(s => s.GetFileVersionsSasUrisAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid[]>()))
            .ReturnsAsync(new Dictionary<Guid, FileVersionSasUriInfo>());

        _handler = new GetFileVersionsQueryHandler(
            _projectFilesServiceMock.Object,
            _userServiceMock.Object,
            _mapperMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetFileVersionsQuery ValidQuery() =>
        new GetFileVersionsQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            Scope = ResourceScope.All
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFileNotAccessible_ThrowsNotFoundApiException()
    {
        // Arrange
        GetFileVersionsQuery query = ValidQuery();
        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectFileCacheDto?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoVersions_ReturnsEmptyList()
    {
        // Arrange
        GetFileVersionsQuery query = ValidQuery();
        ProjectFileCacheDto fileDto = new ProjectFileCacheDto { Id = query.FileId };

        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileDto);

        _projectFilesServiceMock
            .Setup(s => s.GetFileVersionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectFileVersionDto>());

        // Act
        List<ProjectFileVersionWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenVersionsExist_ReturnsMappedVersions()
    {
        // Arrange
        GetFileVersionsQuery query = ValidQuery();
        Guid versionId = Guid.NewGuid();
        Guid createdByUserId = Guid.NewGuid();
        ProjectFileCacheDto fileDto = new ProjectFileCacheDto { Id = query.FileId };

        ProjectFileVersionDto versionDto = new ProjectFileVersionDto
        {
            Id = versionId,
            ProjectFileId = query.FileId,
            VersionNumber = 1,
            CreatedByUserId = createdByUserId,
            BlobFileName = "v1.pdf",
            BlobPath = "path/v1.pdf",
            ContentType = "application/pdf",
            CreatedAt = DateTime.UtcNow
        };

        ProjectFileVersionWeb mappedVersion = new ProjectFileVersionWeb
        {
            Id = versionId,
            ProjectFileId = query.FileId,
            VersionNumber = 1,
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            CreatedByUserName = "Test User",
            SasUrlView = "https://example.com/view",
            SasUrlDownload = "https://example.com/download"
        };

        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFileByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileDto);

        _projectFilesServiceMock
            .Setup(s => s.GetFileVersionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectFileVersionDto> { versionDto });

        _mapperMock
            .Setup(m => m.Map(
                It.IsAny<ProjectFileVersionDto>(),
                It.IsAny<IReadOnlyDictionary<Guid, ProjectMemberUserInfo>>(),
                It.IsAny<FileVersionSasUriInfo?>()))
            .Returns(mappedVersion);

        // Act
        List<ProjectFileVersionWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(versionId);
    }
}
