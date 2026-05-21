using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files;
using CQRS.Files.CreatePackageAndUploadFiles;
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

public sealed class CreatePackageAndUploadFilesCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectFile>> _projectFileRepoMock = new();
    private readonly Mock<IRepository<ProjectFileVersion>> _versionRepoMock = new();
    private readonly Mock<IRepository<ProjectFileVersionComment>> _commentRepoMock = new();
    private readonly Mock<IRepository<ProjectFilePackage>> _packageRepoMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IProjectFilesService> _projectFilesServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<CreatePackageAndUploadFilesCommandHandler>> _loggerMock = new();
    private readonly CreatePackageAndUploadFilesCommandHandler _handler;

    public CreatePackageAndUploadFilesCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreatePackageAndUploadFilesCommandHandler(
            _projectFileRepoMock.Object,
            _versionRepoMock.Object,
            _commentRepoMock.Object,
            _packageRepoMock.Object,
            _blobStorageServiceMock.Object,
            _projectFilesServiceMock.Object,
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

    private static CreatePackageAndUploadFilesCommand ValidCommand(IFormFile? file = null)
    {
        return new CreatePackageAndUploadFilesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageName = "TestPackage",
            Files = new List<FileUploadItem>
            {
                new FileUploadItem { File = file ?? BuildFormFileMock().Object, DisplayName = "Doc.pdf" }
            }
        };
    }

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFilesProvided_InsertsPackageAndFiles()
    {
        // Arrange
        CreatePackageAndUploadFilesCommand command = ValidCommand();

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _packageRepoMock.Verify(r => r.Insert(It.Is<ProjectFilePackage>(
            p => p.Name == command.PackageName
              && p.TenantId == command.TenantId
              && p.ProjectId == command.ProjectId)), Times.Once);
        _projectFileRepoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<ProjectFile>>()), Times.Once);
        _versionRepoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<ProjectFileVersion>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFilesProvided_InvalidatesCache()
    {
        // Arrange
        CreatePackageAndUploadFilesCommand command = ValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _projectFilesServiceMock.Verify(s => s.InvalidateProjectFilesCacheAsync(
            command.TenantId, command.ProjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBlobUploadFails_CompensatesAndRethrows()
    {
        // Arrange
        CreatePackageAndUploadFilesCommand command = ValidCommand();
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
        _packageRepoMock.Verify(r => r.Insert(It.IsAny<ProjectFilePackage>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFileHasComment_InsertsComments()
    {
        // Arrange
        CreatePackageAndUploadFilesCommand command = new CreatePackageAndUploadFilesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PackageName = "TestPackage",
            Files = new List<FileUploadItem>
            {
                new FileUploadItem
                {
                    File = BuildFormFileMock().Object,
                    DisplayName = "Doc.pdf",
                    Comment = "Initial upload comment"
                }
            }
        };

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _commentRepoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<ProjectFileVersionComment>>()), Times.Once);
    }
}
