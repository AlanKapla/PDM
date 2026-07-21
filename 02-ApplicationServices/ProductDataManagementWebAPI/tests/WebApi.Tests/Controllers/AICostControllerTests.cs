using Business.Interfaces.Helpers;
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
    public async Task ParseProjectCostDocument_WhenPdfFileIsValid_SendsQuery_ReturnsOk()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Mock<IFormFile> file = BuildFormFile("invoice.pdf", "application/pdf", AICostImportTestFileBytes.Pdf);

        ParsedCostDto parsed = new ParsedCostDto
        {
            Name = "Test",
            Net = 100m,
            Gross = 123m,
            Confidence = 0.9
        };

        SetupMediatorReturns<ParseCostDocumentQuery, ParsedCostDto>(parsed);

        // Act
        IActionResult result = await _sut.ParseProjectCostDocument(tenantId, projectId, file.Object);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        VerifyMediatorCalledOnce<ParseCostDocumentQuery>(q =>
            q.TenantId == tenantId && q.ProjectId == projectId);
    }

    [Fact]
    public async Task ParseProjectCostDocument_WhenFileHasInvalidExtension_ReturnsBadRequest()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Mock<IFormFile> file = BuildFormFile("invoice.txt", "text/plain", [0x74, 0x65, 0x78, 0x74]);

        // Act
        IActionResult result = await _sut.ParseProjectCostDocument(tenantId, projectId, file.Object);

        // Assert
        BadRequestObjectResult badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be($"Niedozwolony format pliku. {FileContentValidator.AllowedFormatsMessage}");
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

    private static Mock<IFormFile> BuildFormFile(
        string fileName,
        string contentType = "image/jpeg",
        byte[]? content = null)
    {
        byte[] fileContent = content ?? AICostImportTestFileBytes.Jpeg;
        Mock<IFormFile> mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(fileContent.Length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));
        return mock;
    }
}

internal static class AICostImportTestFileBytes
{
    internal static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    internal static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
}
