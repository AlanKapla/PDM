using Business.Interfaces.WebModels.AI;
using CQRS.AI.AcceptAICostImportItem;
using CQRS.AI.AcceptAllAICostImportItems;
using CQRS.AI.GetPendingAICostImportCount;
using CQRS.AI.GetPendingAICostImportItems;
using CQRS.AI.RejectAICostImportItem;
using CQRS.AI.SubmitAICostImportBatch;
using CQRS.AI.ParseCostDocument;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers;

public sealed class AICostControllerTests : ControllerTestBase
{
    private readonly AICostController _sut;

    public AICostControllerTests()
    {
        _sut = new AICostController(MediatorMock.Object);
    }

    [Fact]
    public async Task GetPendingImportItems_SendsQueryWithRouteParams_ReturnsOk()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        List<AICostImportItemWeb> items = new List<AICostImportItemWeb>();

        SetupMediatorReturns<GetPendingAICostImportItemsQuery, IReadOnlyList<AICostImportItemWeb>>(items);

        // Act
        IActionResult result = await _sut.GetPendingImportItems(tenantId, projectId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        VerifyMediatorCalledOnce<GetPendingAICostImportItemsQuery>(q =>
            q.TenantId == tenantId && q.ProjectId == projectId);
    }

    [Fact]
    public async Task GetPendingImportCount_SendsQueryWithRouteParams_ReturnsOk()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        PendingAICostImportCountWeb count = new PendingAICostImportCountWeb
        {
            PendingCount = 2,
            ErrorCount = 0,
            DuplicateCount = 1
        };

        SetupMediatorReturns<GetPendingAICostImportCountQuery, PendingAICostImportCountWeb>(count);

        // Act
        IActionResult result = await _sut.GetPendingImportCount(tenantId, projectId, CancellationToken.None);

        // Assert
        OkObjectResult ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(count);
    }

    [Fact]
    public async Task SubmitImportBatch_WhenNoFiles_ReturnsBadRequest()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        FormFileCollection files = new FormFileCollection();

        // Act
        IActionResult result = await _sut.SubmitImportBatch(
            tenantId,
            projectId,
            files,
            CostDocumentType.ProjectCost,
            null,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitImportBatch_WhenTwoFiles_SendsCommand_ReturnsCreated()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid batchId = Guid.NewGuid();

        FormFileCollection files = new FormFileCollection
        {
            BuildFormFile("a.jpg").Object,
            BuildFormFile("b.jpg").Object
        };

        AICostImportSubmitResultWeb submitResult = new AICostImportSubmitResultWeb
        {
            BatchId = batchId,
            TotalFiles = 2,
            Message = "Documents are being analyzed in the background."
        };

        SetupMediatorReturns<SubmitAICostImportBatchCommand, AICostImportSubmitResultWeb>(submitResult);

        // Act
        IActionResult result = await _sut.SubmitImportBatch(
            tenantId,
            projectId,
            files,
            CostDocumentType.ProjectCost,
            null,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        VerifyMediatorCalledOnce<SubmitAICostImportBatchCommand>(c =>
            c.TenantId == tenantId
            && c.ProjectId == projectId
            && c.Files.Count == 2
            && c.CostDocumentType == CostDocumentType.ProjectCost);
    }

    [Fact]
    public async Task AcceptPendingImportItem_SendsCommandWithRouteParams_ReturnsOk()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();

        AICostImportItemWeb itemWeb = new AICostImportItemWeb
        {
            Id = itemId,
            BatchId = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            Status = "Accepted",
            OriginalFileName = "invoice.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            CostDocumentType = nameof(CostDocumentType.ProjectCost),
            CreatedAt = DateTimeOffset.UtcNow
        };

        SetupMediatorReturns<AcceptAICostImportItemCommand, AICostImportItemWeb>(itemWeb);

        // Act
        IActionResult result = await _sut.AcceptPendingImportItem(
            tenantId, projectId, itemId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        VerifyMediatorCalledOnce<AcceptAICostImportItemCommand>(c =>
            c.TenantId == tenantId && c.ProjectId == projectId && c.ItemId == itemId);
    }

    [Fact]
    public async Task RejectPendingImportItem_SendsCommandWithRouteParams_ReturnsNoContent()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();

        SetupMediatorReturns<RejectAICostImportItemCommand, MediatR.Unit>(MediatR.Unit.Value);

        // Act
        IActionResult result = await _sut.RejectPendingImportItem(
            tenantId, projectId, itemId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        VerifyMediatorCalledOnce<RejectAICostImportItemCommand>(c =>
            c.TenantId == tenantId && c.ProjectId == projectId && c.ItemId == itemId);
    }

    [Fact]
    public async Task AcceptAllPendingImportItems_SendsCommandWithRouteParams_ReturnsOk()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        AICostImportAcceptAllResultWeb acceptAllResult = new AICostImportAcceptAllResultWeb
        {
            AcceptedCount = 2,
            FailedCount = 0
        };

        SetupMediatorReturns<AcceptAllAICostImportItemsCommand, AICostImportAcceptAllResultWeb>(acceptAllResult);

        // Act
        IActionResult result = await _sut.AcceptAllPendingImportItems(
            tenantId, projectId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        VerifyMediatorCalledOnce<AcceptAllAICostImportItemsCommand>(c =>
            c.TenantId == tenantId && c.ProjectId == projectId);
    }

    private static Mock<IFormFile> BuildFormFile(string fileName)
    {
        Mock<IFormFile> mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns("image/jpeg");
        mock.Setup(f => f.Length).Returns(1024);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return mock;
    }
}
