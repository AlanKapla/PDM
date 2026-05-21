using Business.Implementation.Services;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models.Costs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class CostTrackerAttachmentServiceTests
{
    private readonly Mock<IBlobStorageService> _blobMock = new();
    private readonly Mock<IRepository<BaseCostAttachment>> _repoMock = new();
    private readonly CostTrackerAttachmentService _sut;

    private static readonly string ContainerName =
        BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

    public CostTrackerAttachmentServiceTests()
    {
        _sut = new CostTrackerAttachmentService(
            _blobMock.Object,
            _repoMock.Object,
            NullLogger<CostTrackerAttachmentService>.Instance);
    }

    // ─── GenerateFileUrl ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateFileUrl_CallsGenerateSasUri_WithCorrectArguments()
    {
        // Arrange
        BaseCostAttachment attachment = new()
        {
            BlobName = "tenant/proj/cost/file.pdf",
            OriginalFileName = "file.pdf"
        };
        Uri expectedUri = new("https://storage.blob.core.windows.net/sas-token");

        _blobMock
            .Setup(b => b.GenerateSasUri(ContainerName, attachment.BlobName, attachment.OriginalFileName, It.IsAny<int>(), It.IsAny<string>()))
            .Returns(expectedUri);

        // Act
        string result = _sut.GenerateFileUrl(attachment);

        // Assert
        result.Should().Be(expectedUri.ToString());
        _blobMock.Verify(b => b.GenerateSasUri(ContainerName, attachment.BlobName, attachment.OriginalFileName, It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    // ─── SyncAttachmentsAsync — no changes ───────────────────────────────────

    [Fact]
    public async Task SyncAttachmentsAsync_NoNewFiles_NoExistingIds_RetainsCurrentAttachments()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        BaseCost cost = new TestBaseCost();
        BaseCostAttachment existing = new()
        {
            Id = Guid.NewGuid(),
            CostId = cost.Id,
            BlobName = "blob",
            OriginalFileName = "f.pdf"
        };

        _repoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync([existing]);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        List<BaseCostAttachment> result = await _sut.SyncAttachmentsAsync(
            cost, newFiles: null, existingAttachmentIds: null, tenantId, projectId);

        // Assert
        result.Should().ContainSingle(a => a.Id == existing.Id);
        _blobMock.Verify(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _blobMock.Verify(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── SyncAttachmentsAsync — soft-delete removed attachments ──────────────

    [Fact]
    public async Task SyncAttachmentsAsync_AttachmentNotInExistingIds_SoftDeletesAndDeletesBlob()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        BaseCost cost = new TestBaseCost();
        Guid retainedId = Guid.NewGuid();
        Guid removedId = Guid.NewGuid();

        BaseCostAttachment retained = new() { Id = retainedId, BlobName = "keep.pdf", OriginalFileName = "keep.pdf" };
        BaseCostAttachment removed = new() { Id = removedId, BlobName = "remove.pdf", OriginalFileName = "remove.pdf" };

        _repoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync([retained, removed]);
        _repoMock.Setup(r => r.Update(It.IsAny<BaseCostAttachment>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _blobMock.Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        List<BaseCostAttachment> result = await _sut.SyncAttachmentsAsync(
            cost, newFiles: null, existingAttachmentIds: [retainedId], tenantId, projectId);

        // Assert
        removed.IsDeleted.Should().BeTrue();
        removed.DeletedAt.Should().NotBeNull();
        _blobMock.Verify(b => b.DeleteAsync(ContainerName, "remove.pdf", It.IsAny<CancellationToken>()), Times.Once);
        result.Should().ContainSingle(a => a.Id == retainedId);
    }

    // ─── SyncAttachmentsAsync — upload new files ──────────────────────────────

    [Fact]
    public async Task SyncAttachmentsAsync_WithNewFile_UploadsAndInsertsAttachment()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        BaseCost cost = new TestBaseCost();

        _repoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync([]);
        _repoMock.Setup(r => r.Insert(It.IsAny<BaseCostAttachment>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        Mock<IFormFile> fileMock = new();
        MemoryStream stream = new(new byte[] { 1, 2, 3 });
        fileMock.Setup(f => f.FileName).Returns("report.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(3);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        _blobMock
            .Setup(b => b.UploadAsync(ContainerName, It.IsAny<string>(), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        List<BaseCostAttachment> result = await _sut.SyncAttachmentsAsync(
            cost, newFiles: [fileMock.Object], existingAttachmentIds: null, tenantId, projectId);

        // Assert
        _blobMock.Verify(b => b.UploadAsync(ContainerName, It.Is<string>(s => s.Contains("report.pdf")), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.Insert(It.IsAny<BaseCostAttachment>()), Times.Once);
        result.Should().ContainSingle(a => a.OriginalFileName == "report.pdf");
    }

    // ─── SyncAttachmentsAsync — blob delete failure is non-fatal ─────────────

    [Fact]
    public async Task SyncAttachmentsAsync_BlobDeleteThrows_SoftDeleteStillApplied()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        BaseCost cost = new TestBaseCost();
        Guid removedId = Guid.NewGuid();
        BaseCostAttachment removed = new() { Id = removedId, BlobName = "gone.pdf", OriginalFileName = "gone.pdf" };

        _repoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<BaseCostAttachment, bool>>>(),
                It.IsAny<Func<IQueryable<BaseCostAttachment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<BaseCostAttachment, object>>[]>()))
            .ReturnsAsync([removed]);
        _repoMock.Setup(r => r.Update(It.IsAny<BaseCostAttachment>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        _blobMock
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage unavailable"));

        // Act
        Func<Task> act = () => _sut.SyncAttachmentsAsync(
            cost, newFiles: null, existingAttachmentIds: [], tenantId, projectId);

        // Assert — should NOT throw; blob failure is swallowed
        await act.Should().NotThrowAsync();
        removed.IsDeleted.Should().BeTrue();
    }

    // ─── helper ──────────────────────────────────────────────────────────────

    private sealed class TestBaseCost : BaseCost
    {
        public TestBaseCost() { Id = Guid.NewGuid(); }
    }
}
