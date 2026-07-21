using CQRS.CostTrackers.CreateTrackedCost;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Moq;

namespace WebApi.Tests.Validators;

public sealed class CreateTrackedCostCommandValidatorTests
{
    private readonly CreateTrackedCostCommandValidator _sut = new();

    private static CreateTrackedCostCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Name = "Tracked cost",
        Gross = 100m
    };

    [Fact]
    public void NewFiles_InvalidType_FailsValidation()
    {
        IFormFile invalidFile = CreateFormFile(1024, "invoice.txt", "text/plain");
        CreateTrackedCostCommand cmd = ValidCommand() with
        {
            NewFiles = new List<IFormFile> { invalidFile }
        };

        ValidationResult result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("JPEG") || e.ErrorMessage.Contains("PNG") || e.ErrorMessage.Contains("PDF"));
    }

    [Fact]
    public void NewFiles_ValidPdf_PassesValidation()
    {
        IFormFile pdfFile = CreateFormFile(1024, "invoice.pdf", "application/pdf");
        CreateTrackedCostCommand cmd = ValidCommand() with
        {
            NewFiles = new List<IFormFile> { pdfFile }
        };

        ValidationResult result = _sut.Validate(cmd);

        result.Errors.Should().NotContain(e => e.PropertyName.StartsWith("NewFiles"));
    }

    [Fact]
    public void NewFiles_Over50MB_FailsValidation()
    {
        long sizeBytes = 50L * 1024 * 1024 + 1;
        IFormFile largeFile = CreateFormFile(sizeBytes, "invoice.pdf", "application/pdf");
        CreateTrackedCostCommand cmd = ValidCommand() with
        {
            NewFiles = new List<IFormFile> { largeFile }
        };

        ValidationResult result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("50MB"));
    }

    [Fact]
    public void NewFiles_Null_PassesValidation()
    {
        CreateTrackedCostCommand cmd = ValidCommand() with { NewFiles = null };

        ValidationResult result = _sut.Validate(cmd);

        result.Errors.Should().NotContain(e => e.PropertyName.StartsWith("NewFiles"));
    }

    private static IFormFile CreateFormFile(long sizeBytes, string fileName, string contentType)
    {
        Mock<IFormFile> mock = new();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        return mock.Object;
    }
}
