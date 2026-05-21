using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files.AddFileVersionComment;
using Entities.Models.Files;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CQRS.Tests.Files;

public sealed class AddFileVersionCommentCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectFileVersionComment>> _commentRepoMock = new();
    private readonly Mock<IReadRepository<ProjectFileVersion>> _versionRepoMock = new();
    private readonly Mock<IFileAccessGuard> _fileAccessGuardMock = new();
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<AddFileVersionCommentCommandHandler>> _loggerMock = new();
    private readonly AddFileVersionCommentCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public AddFileVersionCommentCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(UserId);

        _handler = new AddFileVersionCommentCommandHandler(
            _commentRepoMock.Object,
            _versionRepoMock.Object,
            _fileAccessGuardMock.Object,
            _projectFilesServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AddFileVersionCommentCommand ValidCommand(Guid? fileId = null, Guid? versionId = null)
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        return new AddFileVersionCommentCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            FileId = fileId ?? Guid.NewGuid(),
            VersionId = versionId ?? Guid.NewGuid(),
            Comment = "Test comment"
        };
    }

    private void SetupVersionRepoReturns(ProjectFileVersion? version)
    {
        // IRepository<T> overload (params includes)
        _versionRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectFileVersion, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFileVersion>, IIncludableQueryable<ProjectFileVersion, object>>[]>()))
            .ReturnsAsync(version);

        // IReadRepository<T> overload (CT + params includes)
        _versionRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectFileVersion, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<ProjectFileVersion>, IIncludableQueryable<ProjectFileVersion, object>>[]>()))
            .ReturnsAsync(version);
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenVersionExistsAndBelongsToFile_InsertsCommentAndInvalidatesCache()
    {
        // Arrange
        AddFileVersionCommentCommand command = ValidCommand();
        ProjectFileVersion version = new ProjectFileVersion
        {
            Id = command.VersionId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            ProjectFileId = command.FileId
        };

        SetupVersionRepoReturns(version);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _commentRepoMock.Verify(r => r.Insert(It.Is<ProjectFileVersionComment>(
            c => c.ProjectFileVersionId == command.VersionId
              && c.Content == command.Comment
              && c.UserId == UserId)), Times.Once);
        _projectFilesServiceMock.Verify(s => s.InvalidateProjectCommentsCacheAsync(
            command.TenantId, command.ProjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVersionNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        AddFileVersionCommentCommand command = ValidCommand();
        SetupVersionRepoReturns(null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenVersionBelongsToDifferentFile_ThrowsNotFoundApiException()
    {
        // Arrange
        AddFileVersionCommentCommand command = ValidCommand();
        ProjectFileVersion version = new ProjectFileVersion
        {
            Id = command.VersionId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            ProjectFileId = Guid.NewGuid() // different file
        };

        SetupVersionRepoReturns(version);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessGuardThrows_PropagatesException()
    {
        // Arrange
        AddFileVersionCommentCommand command = ValidCommand();
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
}
