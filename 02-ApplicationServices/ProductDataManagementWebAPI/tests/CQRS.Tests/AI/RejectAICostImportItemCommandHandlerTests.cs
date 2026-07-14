using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.AI.RejectAICostImportItem;
using Entities.Enums;
using Entities.Models.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.AI;

public sealed class RejectAICostImportItemCommandHandlerTests
{
    private readonly Mock<IRepository<AICostImportItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<AICostImportBatch>> _batchRepoMock = new();
    private readonly Mock<IAICostImportBlobService> _blobServiceMock = new();
    private readonly Mock<IAccessService> _accessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<RejectAICostImportItemCommandHandler>> _loggerMock = new();
    private readonly RejectAICostImportItemCommandHandler _handler;

    public RejectAICostImportItemCommandHandlerTests()
    {
        _accessServiceMock
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<string>(),
                It.IsAny<ResourceRef>(),
                It.IsAny<ResourceScope?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new RejectAICostImportItemCommandHandler(
            _itemRepoMock.Object,
            _batchRepoMock.Object,
            _blobServiceMock.Object,
            _accessServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync((AICostImportItem?)null);

        RejectAICostImportItemCommand command = BuildCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenItemIsPending_DeletesBlobAndItem()
    {
        // Arrange
        AICostImportItem item = AICostImportTestHelpers.BuildItem();
        AICostImportBatch batch = AICostImportTestHelpers.BuildBatch();
        batch.PendingCount = 1;

        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(item);

        _batchRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportBatch, bool>>>()))
            .ReturnsAsync(batch);

        RejectAICostImportItemCommand command = BuildCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        batch.PendingCount.Should().Be(0);
        _blobServiceMock.Verify(
            b => b.DeletePendingAsync(item.BlobPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _itemRepoMock.Verify(r => r.Delete(item), Times.Once);
        _itemRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemAlreadyAccepted_ThrowsConflictApiException()
    {
        // Arrange
        AICostImportItem item = AICostImportTestHelpers.BuildItem(AICostImportItemStatus.Accepted);

        _itemRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(item);

        RejectAICostImportItemCommand command = BuildCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictApiException>();
    }

    private static RejectAICostImportItemCommand BuildCommand() =>
        new RejectAICostImportItemCommand
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId,
            ItemId = AICostImportTestHelpers.ItemId
        };
}
