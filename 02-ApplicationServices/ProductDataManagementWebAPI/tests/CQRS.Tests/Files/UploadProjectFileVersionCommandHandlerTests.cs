using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files.UploadProjectFileVersion;
using Entities.Models.Files;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace CQRS.Tests.Files;

public sealed class UploadProjectFileVersionCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectFile>> _projectFileRepoMock = new();
    private readonly Mock<IRepository<ProjectFileVersion>> _versionRepoMock = new();
    private readonly Mock<IRepository<ProjectFileVersionComment>> _commentRepoMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<IFileAccessGuard> _fileAccessGuardMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<UploadProjectFileVersionCommandHandler>> _loggerMock = new();
    private readonly UploadProjectFileVersionCommandHandler _handler;

    public UploadProjectFileVersionCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _versionRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<ProjectFileVersion, bool>>>(),
                It.IsAny<Expression<Func<ProjectFileVersion, int>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        _handler = new UploadProjectFileVersionCommandHandler(
            _projectFileRepoMock.Object,
            _versionRepoMock.Object,
            _commentRepoMock.Object,
            _blobStorageServiceMock.Object,
            _projectFilesServiceMock.Object,
            _fileAccessGuardMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Mock<IFormFile> BuildFormFileMock(string fileName = "document.pdf")
    {
        Mock<IFormFile> mock = new();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns("application/pdf");
        mock.Setup(f => f.Length).Returns(100);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return mock;
    }

    private static UploadProjectFileVersionCommand ValidCommand(
        string fileName = "document.pdf",
        string? comment = null)
    {
        return new UploadProjectFileVersionCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            File = BuildFormFileMock(fileName).Object,
            Comment = comment
        };
    }

    private void SetupFileRepoReturns(ProjectFile? file)
    {
        _projectFileRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectFile, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectFile>, IIncludableQueryable<ProjectFile, object>>[]>()))
            .ReturnsAsync(file);
    }

    private static ProjectFile BuildProjectFile(
        UploadProjectFileVersionCommand command,
        string fileName = "document.pdf")
    {
        return new ProjectFile
        {
            Id = command.FileId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            FileName = fileName,
            DisplayName = "Document",
            ProjectFilePackageId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Package = new ProjectFilePackage { Name = "TestPackage" }
        };
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFileExists_InsertsVersionAndUpdatesFile()
    {
        // Arrange
        UploadProjectFileVersionCommand command = ValidCommand();
        ProjectFile file = BuildProjectFile(command);
        SetupFileRepoReturns(file);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _versionRepoMock.Verify(r => r.Insert(It.IsAny<ProjectFileVersion>()), Times.Once);
        _projectFileRepoMock.Verify(r => r.Update(file), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommentProvided_InsertsComment()
    {
        // Arrange
        UploadProjectFileVersionCommand command = ValidCommand(comment: "Version 2 changes");
        ProjectFile file = BuildProjectFile(command);
        SetupFileRepoReturns(file);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _commentRepoMock.Verify(r => r.Insert(It.IsAny<ProjectFileVersionComment>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoComment_DoesNotInsertComment()
    {
        // Arrange
        UploadProjectFileVersionCommand command = ValidCommand(comment: null);
        ProjectFile file = BuildProjectFile(command);
        SetupFileRepoReturns(file);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _commentRepoMock.Verify(r => r.Insert(It.IsAny<ProjectFileVersionComment>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFileNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UploadProjectFileVersionCommand command = ValidCommand();
        SetupFileRepoReturns(null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenExtensionMismatch_ThrowsValidationApiException()
    {
        // Arrange
        // File stored as .pdf, but uploading .docx
        UploadProjectFileVersionCommand command = ValidCommand(fileName: "newversion.docx");
        ProjectFile file = BuildProjectFile(command, fileName: "original.pdf");
        SetupFileRepoReturns(file);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenBlobUploadFails_CompensatesAndRethrows()
    {
        // Arrange
        UploadProjectFileVersionCommand command = ValidCommand();
        ProjectFile file = BuildProjectFile(command);
        SetupFileRepoReturns(file);

        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Blob upload failed"));

        _blobStorageServiceMock
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IOException>();
        _versionRepoMock.Verify(r => r.Insert(It.IsAny<ProjectFileVersion>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessGuardThrows_PropagatesException()
    {
        // Arrange
        UploadProjectFileVersionCommand command = ValidCommand();
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
