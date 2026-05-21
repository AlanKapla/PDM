using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files.UpdateFileShare;
using Entities.Models.Files;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CQRS.Tests.Files;

public sealed class UpdateFileShareCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectFile>> _projectFileRepoMock = new();
    private readonly Mock<IRepository<SharedProjectFile>> _sharedProjectFileRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IFileAccessGuard> _fileAccessGuardMock = new();
    private readonly Mock<IFileShareDiffService> _shareDiffServiceMock = new();
    private readonly Mock<IFileShareNotificationService> _notificationsMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<UpdateFileShareCommandHandler>> _loggerMock = new();
    private readonly UpdateFileShareCommandHandler _handler;

    private static readonly Guid CurrentUserId = Guid.NewGuid();

    public UpdateFileShareCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(CurrentUserId);

        _sharedProjectFileRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<SharedProjectFile, bool>>>(),
                It.IsAny<Func<IQueryable<SharedProjectFile>, IIncludableQueryable<SharedProjectFile, object>>[]>()))
            .ReturnsAsync(new List<SharedProjectFile>());

        _shareDiffServiceMock
            .Setup(s => s.Compute(It.IsAny<FileShareDiffInput>()))
            .Returns(new FileShareDiffResult
            {
                SharesToInsert = new List<SharedProjectFile>(),
                SharesToDelete = new List<SharedProjectFile>(),
                UsersGrantedAccess = new List<Guid>(),
                UsersRevokedAccess = new List<Guid>()
            });

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        _handler = new UpdateFileShareCommandHandler(
            _projectFileRepoMock.Object,
            _sharedProjectFileRepoMock.Object,
            _userRepoMock.Object,
            _projectFilesServiceMock.Object,
            _fileAccessGuardMock.Object,
            _shareDiffServiceMock.Object,
            _notificationsMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateFileShareCommand ValidCommand() =>
        new UpdateFileShareCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            SharedWithUserIds = new List<Guid> { Guid.NewGuid() }
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
    public async Task Handle_WhenFileFound_AppliesDiffAndInvalidatesCache()
    {
        // Arrange
        UpdateFileShareCommand command = ValidCommand();
        ProjectFile file = new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = "test.pdf",
            DisplayName = "Test",
            ProjectFilePackageId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid()
        };

        SetupFileRepoReturns(file);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _shareDiffServiceMock.Verify(s => s.Compute(It.IsAny<FileShareDiffInput>()), Times.Once);
        _sharedProjectFileRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _projectFilesServiceMock.Verify(s => s.InvalidateFileAccessCacheAsync(
            command.TenantId, command.ProjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateFileShareCommand command = ValidCommand();
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
        UpdateFileShareCommand command = ValidCommand();
        _fileAccessGuardMock
            .Setup(g => g.EnsureCanAccessFileAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<FileAccessKind>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenApiException("Forbidden"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenSharesToInsertExist_InsertsShares()
    {
        // Arrange
        UpdateFileShareCommand command = ValidCommand();
        ProjectFile file = new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = "test.pdf",
            DisplayName = "Test",
            ProjectFilePackageId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid()
        };
        SetupFileRepoReturns(file);

        List<SharedProjectFile> sharesToInsert = new List<SharedProjectFile>
        {
            new SharedProjectFile { Id = Guid.NewGuid(), SharedWithUserId = Guid.NewGuid() }
        };

        _shareDiffServiceMock
            .Setup(s => s.Compute(It.IsAny<FileShareDiffInput>()))
            .Returns(new FileShareDiffResult
            {
                SharesToInsert = sharesToInsert,
                SharesToDelete = new List<SharedProjectFile>(),
                UsersGrantedAccess = new List<Guid> { sharesToInsert[0].SharedWithUserId },
                UsersRevokedAccess = new List<Guid>()
            });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _sharedProjectFileRepoMock.Verify(r => r.Insert(It.IsAny<SharedProjectFile>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSharesToDeleteExist_DeletesShares()
    {
        // Arrange
        UpdateFileShareCommand command = ValidCommand();
        ProjectFile file = new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = "test.pdf",
            DisplayName = "Test",
            ProjectFilePackageId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid()
        };
        SetupFileRepoReturns(file);

        List<SharedProjectFile> sharesToDelete = new List<SharedProjectFile>
        {
            new SharedProjectFile { Id = Guid.NewGuid() }
        };

        _shareDiffServiceMock
            .Setup(s => s.Compute(It.IsAny<FileShareDiffInput>()))
            .Returns(new FileShareDiffResult
            {
                SharesToInsert = new List<SharedProjectFile>(),
                SharesToDelete = sharesToDelete,
                UsersGrantedAccess = new List<Guid>(),
                UsersRevokedAccess = new List<Guid> { Guid.NewGuid() }
            });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _sharedProjectFileRepoMock.Verify(r => r.DeleteRange(sharesToDelete), Times.Once);
    }
}
