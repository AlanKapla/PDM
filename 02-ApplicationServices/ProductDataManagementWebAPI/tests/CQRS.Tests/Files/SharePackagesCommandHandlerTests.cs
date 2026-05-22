using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files.SharePackages;
using Entities.Models.Files;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CQRS.Tests.Files;

public sealed class SharePackagesCommandHandlerTests
{
    private readonly Mock<IRepository<SharedProjectFile>> _sharedProjectFileRepoMock = new();
    private readonly Mock<IRepository<ProjectFilePackage>> _packageRepoMock = new();
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IFileAccessGuard> _fileAccessGuardMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<SharePackagesCommandHandler>> _loggerMock = new();
    private readonly SharePackagesCommandHandler _handler;

    private static readonly Guid CurrentUserId = Guid.NewGuid();

    public SharePackagesCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(CurrentUserId);

        _sharedProjectFileRepoMock
            .Setup(r => r.ExecuteDeleteAsync(
                It.IsAny<Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _sharedProjectFileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<Func<IQueryable<SharedProjectFile>, IIncludableQueryable<SharedProjectFile, object>>[]>()))
            .ReturnsAsync((SharedProjectFile?)null);

        _handler = new SharePackagesCommandHandler(
            _sharedProjectFileRepoMock.Object,
            _packageRepoMock.Object,
            _projectFilesServiceMock.Object,
            _fileAccessGuardMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SharePackagesCommand BuildCommand(
        Guid? currentUserId = null,
        List<Guid>? packageIds = null,
        List<Guid>? sharedWithUserIds = null,
        List<ProjectFilePackage>? packages = null)
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        return new SharePackagesCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            PackageIds = packageIds ?? new List<Guid> { Guid.NewGuid() },
            SharedWithUserIds = sharedWithUserIds ?? new List<Guid> { Guid.NewGuid() }
        };
    }

    private void SetupPackages(SharePackagesCommand command, List<ProjectFilePackage> packages)
    {
        _packageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectFilePackage, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFilePackage>, IIncludableQueryable<ProjectFilePackage, object>>[]>()))
            .ReturnsAsync(packages);
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidRequest_InsertsShareAndSavesChanges()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid packageOwnerId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();

        SharePackagesCommand command = new SharePackagesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageIds = new List<Guid> { packageId },
            SharedWithUserIds = new List<Guid> { targetUserId }
        };

        List<ProjectFilePackage> packages = new List<ProjectFilePackage>
        {
            new ProjectFilePackage { Id = packageId, OwnerId = packageOwnerId }
        };

        SetupPackages(command, packages);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _sharedProjectFileRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sharedProjectFileRepoMock.Verify(r => r.Insert(It.IsAny<SharedProjectFile>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsSelf_SkipsShare()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid packageOwnerId = Guid.NewGuid();

        SharePackagesCommand command = new SharePackagesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageIds = new List<Guid> { packageId },
            SharedWithUserIds = new List<Guid> { CurrentUserId } // same as current user
        };

        SetupPackages(command, new List<ProjectFilePackage>
        {
            new ProjectFilePackage { Id = packageId, OwnerId = packageOwnerId }
        });

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _sharedProjectFileRepoMock.Verify(r => r.Insert(It.IsAny<SharedProjectFile>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsPackageOwner_SkipsShare()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid packageOwnerId = Guid.NewGuid();

        SharePackagesCommand command = new SharePackagesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageIds = new List<Guid> { packageId },
            SharedWithUserIds = new List<Guid> { packageOwnerId } // same as owner
        };

        SetupPackages(command, new List<ProjectFilePackage>
        {
            new ProjectFilePackage { Id = packageId, OwnerId = packageOwnerId }
        });

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _sharedProjectFileRepoMock.Verify(r => r.Insert(It.IsAny<SharedProjectFile>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessGuardThrows_PropagatesException()
    {
        // Arrange
        SharePackagesCommand command = BuildCommand();
        _fileAccessGuardMock
            .Setup(g => g.EnsureCanAccessPackageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<FileAccessKind>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenApiException("Forbidden"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenShareAlreadyExists_DoesNotInsertDuplicate()
    {
        // Arrange
        Guid packageId = Guid.NewGuid();
        Guid packageOwnerId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();

        SharePackagesCommand command = new SharePackagesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageIds = new List<Guid> { packageId },
            SharedWithUserIds = new List<Guid> { targetUserId }
        };

        SetupPackages(command, new List<ProjectFilePackage>
        {
            new ProjectFilePackage { Id = packageId, OwnerId = packageOwnerId }
        });

        // Existing share already exists
        _sharedProjectFileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<Func<IQueryable<SharedProjectFile>, IIncludableQueryable<SharedProjectFile, object>>[]>()))
            .ReturnsAsync(new SharedProjectFile { Id = Guid.NewGuid() });

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _sharedProjectFileRepoMock.Verify(r => r.Insert(It.IsAny<SharedProjectFile>()), Times.Never);
    }
}
