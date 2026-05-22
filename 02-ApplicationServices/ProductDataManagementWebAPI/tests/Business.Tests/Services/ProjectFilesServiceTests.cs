using Business.Implementation.Services;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class ProjectFilesServiceTests
{
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<IAccessService> _accessMock = new();
    private readonly Mock<IReadRepository<SharedProjectFile>> _sharedFileRepoMock = new();
    private readonly Mock<IReadRepository<ProjectFile>> _fileRepoMock = new();
    private readonly Mock<IReadRepository<ProjectFilePackage>> _packageRepoMock = new();
    private readonly Mock<IReadRepository<ProjectFileVersion>> _versionRepoMock = new();
    private readonly Mock<IReadRepository<ProjectFileVersionComment>> _commentRepoMock = new();
    private readonly Mock<IBlobStorageService> _blobMock = new();
    private readonly ProjectFilesService _sut;

    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public ProjectFilesServiceTests()
    {
        _sut = new ProjectFilesService(
            _cacheMock.Object,
            _accessMock.Object,
            _sharedFileRepoMock.Object,
            _fileRepoMock.Object,
            _packageRepoMock.Object,
            _versionRepoMock.Object,
            _commentRepoMock.Object,
            _blobMock.Object,
            NullLogger<ProjectFilesService>.Instance);

        _userMock.Setup(u => u.Id).Returns(_userId);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private void SetupFilesCache(Dictionary<Guid, List<ProjectFileCacheDto>> data)
    {
        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.Is<string>(k => k.Contains("project:files:files")),
                It.IsAny<Func<Task<Dictionary<Guid, List<ProjectFileCacheDto>>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
    }

    private void SetupPackagesCache(Dictionary<Guid, ProjectFilePackageDto> data)
    {
        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.Is<string>(k => k.Contains("project:files:packages")),
                It.IsAny<Func<Task<Dictionary<Guid, ProjectFilePackageDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
    }

    // ─── GetAccessibleFileCountsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetAccessibleFileCountsAsync_EmptyPackageIds_ReturnsEmptyDictionary()
    {
        // Act
        Dictionary<Guid, int> result = await _sut.GetAccessibleFileCountsAsync(
            _userMock.Object, _tenantId, _projectId, new HashSet<Guid>(), ResourceScope.All);

        // Assert
        result.Should().BeEmpty();
        _cacheMock.Verify(c => c.GetOrAddAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<Dictionary<Guid, int>>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAccessibleFileCountsAsync_ScopeAll_CountsAllFilesPerPackage()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        List<ProjectFileCacheDto> files =
        [
            new() { Id = Guid.NewGuid(), ProjectFilePackageId = packageId },
            new() { Id = Guid.NewGuid(), ProjectFilePackageId = packageId },
        ];
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>> { [packageId] = files });

        // Act
        Dictionary<Guid, int> result = await _sut.GetAccessibleFileCountsAsync(
            _userMock.Object, _tenantId, _projectId, new HashSet<Guid> { packageId }, ResourceScope.All);

        // Assert
        result.Should().ContainKey(packageId);
        result[packageId].Should().Be(2);
    }

    [Fact]
    public async Task GetAccessibleFileCountsAsync_ScopeMine_CountsOnlyOwnedFiles()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        List<ProjectFileCacheDto> files =
        [
            new() { Id = Guid.NewGuid(), ProjectFilePackageId = packageId, OwnerId = _userId },
            new() { Id = Guid.NewGuid(), ProjectFilePackageId = packageId, OwnerId = otherUserId },
        ];
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>> { [packageId] = files });

        // Act
        Dictionary<Guid, int> result = await _sut.GetAccessibleFileCountsAsync(
            _userMock.Object, _tenantId, _projectId, new HashSet<Guid> { packageId }, ResourceScope.Mine);

        // Assert
        result[packageId].Should().Be(1);
    }

    [Fact]
    public async Task GetAccessibleFileCountsAsync_ScopeAll_PackageNotInCache_CountIsZero()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>());

        // Act
        Dictionary<Guid, int> result = await _sut.GetAccessibleFileCountsAsync(
            _userMock.Object, _tenantId, _projectId, new HashSet<Guid> { packageId }, ResourceScope.All);

        // Assert
        result[packageId].Should().Be(0);
    }

    // ─── GetPackageAccessInfoAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPackageAccessInfoAsync_ScopeAll_ReturnsFullAccessWithoutCacheCall()
    {
        // Act
        PackageAccessInfo result = await _sut.GetPackageAccessInfoAsync(
            _userMock.Object, _tenantId, _projectId, Guid.NewGuid(), ResourceScope.All);

        // Assert
        result.IsPackageShared.Should().BeTrue();
        result.ExcludedFileIds.Should().BeEmpty();
        result.AllowedFileIds.Should().BeEmpty();
        _cacheMock.Verify(c => c.GetOrAddAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<PackageAccessInfo>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPackageAccessInfoAsync_ScopeMine_ReturnsOnlyOwnedFileIds()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid ownedFileId = Guid.NewGuid();
        Guid otherFileId = Guid.NewGuid();
        List<ProjectFileCacheDto> files =
        [
            new() { Id = ownedFileId, OwnerId = _userId, ProjectFilePackageId = packageId },
            new() { Id = otherFileId, OwnerId = Guid.NewGuid(), ProjectFilePackageId = packageId },
        ];
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>> { [packageId] = files });

        // Act
        PackageAccessInfo result = await _sut.GetPackageAccessInfoAsync(
            _userMock.Object, _tenantId, _projectId, packageId, ResourceScope.Mine);

        // Assert
        result.IsPackageShared.Should().BeFalse();
        result.AllowedFileIds.Should().Contain(ownedFileId);
        result.AllowedFileIds.Should().NotContain(otherFileId);
    }

    // ─── GetAccessibleFilesAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAccessibleFilesAsync_PackageNotInCache_ReturnsEmptyList()
    {
        // Arrange
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>());

        // Act
        List<ProjectFileCacheDto> result = await _sut.GetAccessibleFilesAsync(
            _userMock.Object, _tenantId, _projectId, Guid.NewGuid(), ResourceScope.All);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccessibleFilesAsync_ScopeAll_ReturnsAllFilesInPackage()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        List<ProjectFileCacheDto> files =
        [
            new() { Id = Guid.NewGuid(), OwnerId = _userId, ProjectFilePackageId = packageId },
            new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), ProjectFilePackageId = packageId },
        ];
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>> { [packageId] = files });

        // Act
        List<ProjectFileCacheDto> result = await _sut.GetAccessibleFilesAsync(
            _userMock.Object, _tenantId, _projectId, packageId, ResourceScope.All);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccessibleFilesAsync_ScopeMine_ReturnsOnlyOwnedFiles()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid ownedId = Guid.NewGuid();
        List<ProjectFileCacheDto> files =
        [
            new() { Id = ownedId, OwnerId = _userId, ProjectFilePackageId = packageId },
            new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), ProjectFilePackageId = packageId },
        ];
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>> { [packageId] = files });

        // Act
        List<ProjectFileCacheDto> result = await _sut.GetAccessibleFilesAsync(
            _userMock.Object, _tenantId, _projectId, packageId, ResourceScope.Mine);

        // Assert
        result.Should().ContainSingle(f => f.Id == ownedId);
    }

    // ─── GetAccessiblePackagesAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetAccessiblePackagesAsync_NoPackagesInCache_ReturnsEmpty()
    {
        // Arrange
        SetupPackagesCache(new Dictionary<Guid, ProjectFilePackageDto>());

        // Act
        Dictionary<Guid, ProjectFilePackageDto> result = await _sut.GetAccessiblePackagesAsync(
            _userMock.Object, _tenantId, _projectId, ResourceScope.All);

        // Assert
        result.Should().BeEmpty();
    }

    // ─── GetAccessibleFileByIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetAccessibleFileByIdAsync_FileNotInCache_ReturnsNull()
    {
        // Arrange
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>());

        // Act
        ProjectFileCacheDto? result = await _sut.GetAccessibleFileByIdAsync(
            _userMock.Object, _tenantId, _projectId, Guid.NewGuid(), ResourceScope.All);

        // Assert
        result.Should().BeNull();
    }

    // ─── GetFileByIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetFileByIdAsync_FileExists_ReturnsFile()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>
        {
            [packageId] = [new ProjectFileCacheDto { Id = fileId, ProjectFilePackageId = packageId }]
        });

        // Act
        ProjectFileCacheDto? result = await _sut.GetFileByIdAsync(_tenantId, _projectId, fileId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(fileId);
    }

    [Fact]
    public async Task GetFileByIdAsync_FileNotFound_ReturnsNull()
    {
        // Arrange
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>());

        // Act
        ProjectFileCacheDto? result = await _sut.GetFileByIdAsync(_tenantId, _projectId, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ─── HasAccessToFileAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task HasAccessToFileAsync_ScopeAll_FileExists_ReturnsTrue()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>
        {
            [packageId] = [new ProjectFileCacheDto { Id = fileId, ProjectFilePackageId = packageId }]
        });

        // Act
        bool result = await _sut.HasAccessToFileAsync(
            _userMock.Object, _tenantId, _projectId, packageId, fileId, ResourceScope.All);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToFileAsync_ScopeAll_FileNotExists_ReturnsFalse()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>
        {
            [packageId] = []
        });

        // Act
        bool result = await _sut.HasAccessToFileAsync(
            _userMock.Object, _tenantId, _projectId, packageId, Guid.NewGuid(), ResourceScope.All);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessToFileAsync_ScopeMine_FileOwnedByUser_ReturnsTrue()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>
        {
            [packageId] = [new ProjectFileCacheDto { Id = fileId, OwnerId = _userId, ProjectFilePackageId = packageId }]
        });

        // Act
        bool result = await _sut.HasAccessToFileAsync(
            _userMock.Object, _tenantId, _projectId, packageId, fileId, ResourceScope.Mine);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToFileAsync_ScopeMine_FileOwnedByOtherUser_ReturnsFalse()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        SetupFilesCache(new Dictionary<Guid, List<ProjectFileCacheDto>>
        {
            [packageId] = [new ProjectFileCacheDto { Id = fileId, OwnerId = Guid.NewGuid(), ProjectFilePackageId = packageId }]
        });

        // Act
        bool result = await _sut.HasAccessToFileAsync(
            _userMock.Object, _tenantId, _projectId, packageId, fileId, ResourceScope.Mine);

        // Assert
        result.Should().BeFalse();
    }

    // ─── GetFileVersionsSummaryAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetFileVersionsSummaryAsync_EmptyFiles_ReturnsEmptySummary()
    {
        // Act
        FileVersionsSummary result = await _sut.GetFileVersionsSummaryAsync(
            _tenantId, _projectId, []);

        // Assert
        result.VersionCounts.Should().BeEmpty();
        result.CurrentVersionIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFileVersionsSummaryAsync_FilesWithNoVersions_CountsAreZero()
    {
        // Arrange
        Guid fileId = Guid.NewGuid();
        List<ProjectFileCacheDto> files = [new() { Id = fileId }];

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.Is<string>(k => k.Contains("project:files:versions")),
                It.IsAny<Func<Task<Dictionary<Guid, List<ProjectFileVersionDto>>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<ProjectFileVersionDto>>());

        // Act
        FileVersionsSummary result = await _sut.GetFileVersionsSummaryAsync(
            _tenantId, _projectId, files);

        // Assert
        result.VersionCounts[fileId].Should().Be(0);
    }

    [Fact]
    public async Task GetFileVersionsSummaryAsync_FileWithCurrentVersion_IncludesInCurrentVersionIds()
    {
        // Arrange
        Guid fileId = Guid.NewGuid();
        Guid currentVersionId = Guid.NewGuid();
        List<ProjectFileCacheDto> files = [new() { Id = fileId, CurrentVersionId = currentVersionId }];

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.Is<string>(k => k.Contains("project:files:versions")),
                It.IsAny<Func<Task<Dictionary<Guid, List<ProjectFileVersionDto>>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<ProjectFileVersionDto>>());

        // Act
        FileVersionsSummary result = await _sut.GetFileVersionsSummaryAsync(
            _tenantId, _projectId, files);

        // Assert
        result.CurrentVersionIds.Should().Contain(currentVersionId);
    }

    // ─── Cache invalidation methods ───────────────────────────────────────────

    [Fact]
    public async Task InvalidateProjectFilesCacheAsync_RemovesPackagesAndFilesKeys()
    {
        // Arrange
        string packagesKey = $"project:files:packages:{_tenantId}:{_projectId}";
        string filesKey = $"project:files:files:{_tenantId}:{_projectId}";
        _cacheMock.Setup(c => c.RemoveCacheByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateProjectFilesCacheAsync(_tenantId, _projectId);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(packagesKey, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(filesKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateProjectVersionsCacheAsync_RemovesVersionsKey()
    {
        // Arrange
        string versionsCacheKey = $"project:files:versions:{_tenantId}:{_projectId}";
        _cacheMock.Setup(c => c.RemoveCacheByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateProjectVersionsCacheAsync(_tenantId, _projectId);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(versionsCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateFileAccessCacheAsync_CallsRemoveCacheContains()
    {
        // Arrange
        _cacheMock.Setup(c => c.RemoveCacheContainsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateFileAccessCacheAsync(_tenantId, _projectId);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheContainsAsync(
            It.Is<string>(k => k.Contains(_tenantId.ToString()) && k.Contains(_projectId.ToString())),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateVersionSasUriAsync_RemovesSasUriKey()
    {
        // Arrange
        Guid versionId = Guid.NewGuid();
        string expectedKey = $"fileversion:sas:{versionId}";
        _cacheMock.Setup(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateVersionSasUriAsync(versionId);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── GetFileVersionsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFileVersionsAsync_NoVersionsForFile_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.Is<string>(k => k.Contains("project:files:versions")),
                It.IsAny<Func<Task<Dictionary<Guid, List<ProjectFileVersionDto>>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<ProjectFileVersionDto>>());

        // Act
        List<ProjectFileVersionDto> result = await _sut.GetFileVersionsAsync(_tenantId, _projectId, Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    // ─── GetVersionsByIdsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetVersionsByIdsAsync_EmptyIds_ReturnsEmptyResult()
    {
        // Act
        ProjectFileVersionsResult result = await _sut.GetVersionsByIdsAsync(
            _tenantId, _projectId, new HashSet<Guid>());

        // Assert
        result.Versions.Should().BeEmpty();
        result.CreatedByUserIds.Should().BeEmpty();
    }
}
