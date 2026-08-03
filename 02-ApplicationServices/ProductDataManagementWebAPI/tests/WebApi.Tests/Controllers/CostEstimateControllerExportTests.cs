using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.ExportCostEstimate;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers;

public sealed class CostEstimateControllerExportTests : ControllerTestBase
{
    private readonly CostEstimateController _sut;

    public CostEstimateControllerExportTests()
    {
        _sut = new CostEstimateController(MediatorMock.Object);
    }

    [Fact]
    public async Task ExportXlsx_WhenQuerySucceeds_ReturnsFileContentResult()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid id = Guid.NewGuid();
        CostEstimateExportFile file = new(
            Content: [1, 2, 3],
            ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName: "ce_20260721.xlsx");

        SetupMediatorReturns<ExportCostEstimateQuery, CostEstimateExportFile>(file);

        // Act
        IActionResult result = await _sut.ExportXlsx(tenantId, projectId, id);

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be(file.ContentType);
        fileResult.FileDownloadName.Should().Be(file.FileName);
        fileResult.FileContents.Should().Equal(file.Content);

        VerifyMediatorCalledOnce<ExportCostEstimateQuery>(q =>
            q.TenantId == tenantId &&
            q.ProjectId == projectId &&
            q.CostEstimateId == id &&
            q.Format == CostEstimateExportFormat.Xlsx);
    }

    [Fact]
    public async Task ExportPdf_WhenQuerySucceeds_ReturnsFileContentResult()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid id = Guid.NewGuid();
        CostEstimateExportFile file = new(
            Content: [9, 8, 7],
            ContentType: "application/pdf",
            FileName: "ce_20260721.pdf");

        SetupMediatorReturns<ExportCostEstimateQuery, CostEstimateExportFile>(file);

        // Act
        IActionResult result = await _sut.ExportPdf(tenantId, projectId, id);

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be(file.FileName);

        VerifyMediatorCalledOnce<ExportCostEstimateQuery>(q =>
            q.Format == CostEstimateExportFormat.Pdf);
    }
}
