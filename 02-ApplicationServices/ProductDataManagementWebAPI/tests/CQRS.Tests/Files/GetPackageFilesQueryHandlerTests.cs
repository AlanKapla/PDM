using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using CQRS.Files.GetPackageFiles;
using Entities.Models.Files;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.Files;

public sealed class GetPackageFilesQueryHandlerTests
{
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IFileVersionWebMapper> _mapperMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetPackageFilesQueryHandler _handler;

    public GetPackageFilesQueryHandlerTests()
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

        _projectFilesServiceMock
            .Setup(s => s.GetSharedWithUsersAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<Guid>>());

        _projectFilesServiceMock
            .Setup(s => s.GetVersionsByIdsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<HashSet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectFileVersionsResult());

        _projectFilesServiceMock
            .Setup(s => s.GetFileVersionsSummaryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<List<ProjectFileCacheDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileVersionsSummary());

        _handler = new GetPackageFilesQueryHandler(
            _projectFilesServiceMock.Object,
            _userServiceMock.Object,
            _mapperMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetPackageFilesQuery ValidQuery() =>
        new GetPackageFilesQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            Scope = ResourceScope.All
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPackageNotAccessible_ThrowsNotFoundApiException()
    {
        // Arrange
        GetPackageFilesQuery query = ValidQuery();
        _projectFilesServiceMock
            .Setup(s => s.GetAccessiblePackageByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectFilePackageDto?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoFiles_ReturnsEmptyList()
    {
        // Arrange
        GetPackageFilesQuery query = ValidQuery();
        ProjectFilePackageDto packageDto = new ProjectFilePackageDto
        {
            Id = query.PackageId,
            TenantId = query.TenantId,
            ProjectId = query.ProjectId,
            Name = "TestPackage",
            OwnerId = Guid.NewGuid()
        };

        _projectFilesServiceMock
            .Setup(s => s.GetAccessiblePackageByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(packageDto);

        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFilesAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectFileCacheDto>());

        // Act
        List<ProjectFileWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenFilesExist_CallsServiceMethods()
    {
        // Arrange
        GetPackageFilesQuery query = ValidQuery();
        ProjectFilePackageDto packageDto = new ProjectFilePackageDto
        {
            Id = query.PackageId,
            TenantId = query.TenantId,
            ProjectId = query.ProjectId,
            Name = "TestPackage",
            OwnerId = Guid.NewGuid()
        };
        List<ProjectFileCacheDto> files = new List<ProjectFileCacheDto>
        {
            new ProjectFileCacheDto
            {
                Id = Guid.NewGuid(),
                TenantId = query.TenantId,
                ProjectId = query.ProjectId,
                ProjectFilePackageId = query.PackageId,
                FileName = "test.pdf",
                DisplayName = "Test",
                OwnerId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            }
        };

        _projectFilesServiceMock
            .Setup(s => s.GetAccessiblePackageByIdAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(packageDto);

        _projectFilesServiceMock
            .Setup(s => s.GetAccessibleFilesAsync(
                It.IsAny<ICurrentUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<ResourceScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _projectFilesServiceMock.Verify(s => s.GetFileVersionsSummaryAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<List<ProjectFileCacheDto>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
