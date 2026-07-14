using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.GetPendingAICostImportItems;
using Entities.Enums;
using Entities.Models.AI;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.AI;

public sealed class GetPendingAICostImportItemsQueryHandlerTests
{
    private readonly Mock<IReadRepository<AICostImportItem>> _itemRepoMock = new();
    private readonly Mock<IReadRepository<AICostImportBatch>> _batchRepoMock = new();
    private readonly Mock<IAICostImportBlobService> _blobServiceMock = new();
    private readonly Mock<IAccessService> _accessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetPendingAICostImportItemsQueryHandler _handler;

    public GetPendingAICostImportItemsQueryHandlerTests()
    {
        _accessServiceMock
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<string>(),
                It.IsAny<ResourceRef>(),
                It.IsAny<ResourceScope?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _blobServiceMock
            .Setup(b => b.GeneratePendingPreviewUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://preview.example.com/file.jpg");

        _handler = new GetPendingAICostImportItemsQueryHandler(
            _itemRepoMock.Object,
            _batchRepoMock.Object,
            _blobServiceMock.Object,
            _accessServiceMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPendingItemsExist_ReturnsMappedItems()
    {
        // Arrange
        AICostImportItem item = AICostImportTestHelpers.BuildItem();
        AICostImportBatch batch = AICostImportTestHelpers.BuildBatch();

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(new List<AICostImportItem> { item });

        _batchRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportBatch, bool>>>()))
            .ReturnsAsync(batch);

        GetPendingAICostImportItemsQuery query = new GetPendingAICostImportItemsQuery
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId
        };

        // Act
        IReadOnlyList<AICostImportItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(item.Id);
        result[0].Status.Should().Be(nameof(AICostImportItemStatus.Pending));
        result[0].ParsedData.Should().NotBeNull();
        result[0].PreviewUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthorizedForBatch_ReturnsEmptyList()
    {
        // Arrange
        AICostImportItem item = AICostImportTestHelpers.BuildItem();
        AICostImportBatch batch = AICostImportTestHelpers.BuildBatch();

        _itemRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<AICostImportItem, bool>>>()))
            .ReturnsAsync(new List<AICostImportItem> { item });

        _batchRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<AICostImportBatch, bool>>>()))
            .ReturnsAsync(batch);

        _accessServiceMock
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<string>(),
                It.IsAny<ResourceRef>(),
                It.IsAny<ResourceScope?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        GetPendingAICostImportItemsQuery query = new GetPendingAICostImportItemsQuery
        {
            TenantId = AICostImportTestHelpers.TenantId,
            ProjectId = AICostImportTestHelpers.ProjectId
        };

        // Act
        IReadOnlyList<AICostImportItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
