using Business.Implementation.Services;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class FileAccessGuardTests
{
    private readonly Mock<IReadRepository<ProjectFile>> _fileRepoMock = new();
    private readonly Mock<IReadRepository<ProjectFilePackage>> _packageRepoMock = new();
    private readonly Mock<IReadRepository<SharedProjectFile>> _sharedRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly FileAccessGuard _sut;

    public FileAccessGuardTests()
    {
        _sut = new FileAccessGuard(
            _fileRepoMock.Object,
            _packageRepoMock.Object,
            _sharedRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ProjectFile BuildFile(Guid tenantId, Guid projectId, Guid ownerId)
        => new ProjectFile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            OwnerId = ownerId,
            FileName = "test.pdf"
        };

    private static ProjectFilePackage BuildPackage(Guid tenantId, Guid projectId, Guid ownerId)
        => new ProjectFilePackage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            OwnerId = ownerId,
            Name = "Test Package"
        };

    // ─── EnsureCanAccessFileAsync ─────────────────────────────────────────────

    [Fact]
    public async Task EnsureCanAccessFileAsync_FileNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();

        _fileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectFile?)null);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessFileAsync(
            tenantId, projectId, fileId, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task EnsureCanAccessFileAsync_UserIsAdmin_DoesNotThrow()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        ProjectFile file = BuildFile(tenantId, projectId, ownerId);

        _fileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessFileAsync(
            tenantId, projectId, file.Id, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanAccessFileAsync_UserIsOwner_DoesNotThrow()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectFile file = BuildFile(tenantId, projectId, ownerId: userId);

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _fileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessFileAsync(
            tenantId, projectId, file.Id, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(FileAccessKind.Read)]
    [InlineData(FileAccessKind.Write)]
    public async Task EnsureCanAccessFileAsync_UserHasShareAccess_DoesNotThrow(FileAccessKind kind)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectFile file = BuildFile(tenantId, projectId, ownerId);

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _fileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sharedRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessFileAsync(
            tenantId, projectId, file.Id, kind, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanAccessFileAsync_NoAccess_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectFile file = BuildFile(tenantId, projectId, ownerId);

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _fileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sharedRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessFileAsync(
            tenantId, projectId, file.Id, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    // ─── EnsureCanAccessPackageAsync ──────────────────────────────────────────

    [Fact]
    public async Task EnsureCanAccessPackageAsync_PackageNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid packageId = Guid.NewGuid();

        _packageRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFilePackage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectFilePackage?)null);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessPackageAsync(
            tenantId, projectId, packageId, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task EnsureCanAccessPackageAsync_UserIsAdmin_DoesNotThrow()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        ProjectFilePackage package = BuildPackage(tenantId, projectId, ownerId);

        _packageRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFilePackage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessPackageAsync(
            tenantId, projectId, package.Id, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanAccessPackageAsync_UserIsOwner_DoesNotThrow()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectFilePackage package = BuildPackage(tenantId, projectId, ownerId: userId);

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _packageRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFilePackage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessPackageAsync(
            tenantId, projectId, package.Id, FileAccessKind.Write, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanAccessPackageAsync_NotOwnerNotAdmin_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectFilePackage package = BuildPackage(tenantId, projectId, ownerId);

        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _packageRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectFilePackage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.EnsureCanAccessPackageAsync(
            tenantId, projectId, package.Id, FileAccessKind.Read, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
