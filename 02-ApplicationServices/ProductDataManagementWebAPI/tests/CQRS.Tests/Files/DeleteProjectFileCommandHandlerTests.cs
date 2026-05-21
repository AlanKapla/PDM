using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files.DeleteProjectFile;
using Entities.Models.Files;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CQRS.Tests.Files;

public sealed class DeleteProjectFileCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectFile>> _projectFileRepoMock = new();
    private readonly Mock<IReadRepository<SharedProjectFile>> _sharedFileRepoMock = new();
    private readonly Mock<IRepository<ProjectFileVersion>> _versionRepoMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IFileAccessGuard> _fileAccessGuardMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<DeleteProjectFileCommandHandler>> _loggerMock = new();
    private readonly DeleteProjectFileCommandHandler _handler;

    public DeleteProjectFileCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _blobStorageServiceMock
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _versionRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectFileVersion, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFileVersion>, IIncludableQueryable<ProjectFileVersion, object>>[]>()))
            .ReturnsAsync(new List<ProjectFileVersion>());

        _sharedFileRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new DeleteProjectFileCommandHandler(
            _projectFileRepoMock.Object,
            _sharedFileRepoMock.Object,
            _versionRepoMock.Object,
            _blobStorageServiceMock.Object,
            _projectFilesServiceMock.Object,
            _fileAccessGuardMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DeleteProjectFileCommand ValidCommand() =>
        new DeleteProjectFileCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = Guid.NewGuid()
        };

    private void SetupFileRepoReturns(ProjectFile? file)
    {
        _projectFileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFile>, IIncludableQueryable<ProjectFile, object>>[]>()))
            .ReturnsAsync(file);
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFileExists_SoftDeletesFileAndVersions()
    {
        // Arrange
        DeleteProjectFileCommand command = ValidCommand();
        ProjectFile file = new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = "test.pdf",
            DisplayName = "Test"
        };
        ProjectFileVersion version = new ProjectFileVersion
        {
            Id = Guid.NewGuid(),
            ProjectFileId = file.Id,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            BlobPath = "blob/path/test.pdf"
        };

        SetupFileRepoReturns(file);
        _versionRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectFileVersion, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFileVersion>, IIncludableQueryable<ProjectFileVersion, object>>[]>()))
            .ReturnsAsync(new List<ProjectFileVersion> { version });

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        file.IsDeleted.Should().BeTrue();
        version.IsDeleted.Should().BeTrue();
        _projectFileRepoMock.Verify(r => r.Update(file), Times.Once);
        _versionRepoMock.Verify(r => r.Update(version), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFileExists_InvalidatesCaches()
    {
        // Arrange
        DeleteProjectFileCommand command = ValidCommand();
        ProjectFile file = new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = "test.pdf",
            DisplayName = "Test"
        };

        SetupFileRepoReturns(file);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _projectFilesServiceMock.Verify(s => s.InvalidateProjectFilesCacheAsync(
            command.TenantId, command.ProjectId, It.IsAny<CancellationToken>()), Times.Once);
        _projectFilesServiceMock.Verify(s => s.InvalidateProjectVersionsCacheAsync(
            command.TenantId, command.ProjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DeleteProjectFileCommand command = ValidCommand();
        SetupFileRepoReturns(null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessGuardThrows_PropagatesException()
    {
        // Arrange
        DeleteProjectFileCommand command = ValidCommand();
        _fileAccessGuardMock
            .Setup(g => g.EnsureCanAccessFileAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<FileAccessKind>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundApiException(nameof(ProjectFile), command.FileId.ToString()));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenVersionBlobDeleteFails_ContinuesSoftDelete()
    {
        // Arrange
        DeleteProjectFileCommand command = ValidCommand();
        ProjectFile file = new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = "test.pdf",
            DisplayName = "Test"
        };
        ProjectFileVersion version = new ProjectFileVersion
        {
            Id = Guid.NewGuid(),
            ProjectFileId = file.Id,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            BlobPath = "blob/path/test.pdf"
        };

        SetupFileRepoReturns(file);
        _versionRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectFileVersion, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFileVersion>, IIncludableQueryable<ProjectFileVersion, object>>[]>()))
            .ReturnsAsync(new List<ProjectFileVersion> { version });

        _blobStorageServiceMock
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Blob error"));

        // Act - should NOT throw (blob errors are swallowed)
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        version.IsDeleted.Should().BeTrue();
    }
}
