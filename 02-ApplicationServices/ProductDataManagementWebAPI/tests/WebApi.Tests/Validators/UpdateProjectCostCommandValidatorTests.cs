using CQRS.ProjectCosts.UpdateProjectCost;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Moq;

namespace WebApi.Tests.Validators;

public class UpdateProjectCostCommandValidatorTests
{
    private readonly UpdateProjectCostCommandValidator _sut = new();

    private static UpdateProjectCostCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostId = Guid.NewGuid(),
        Name = "Koszt budowlany",
        Gross = 100m
    };

    // ─── Required IDs ────────────────────────────────────────────────────────

    [Fact]
    public void TenantId_Empty_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { TenantId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void ProjectId_Empty_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { ProjectId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    [Fact]
    public void CostId_Empty_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { CostId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CostId");
    }

    // ─── Name ────────────────────────────────────────────────────────────────

    [Fact]
    public void Name_Empty_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Name = "" };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Exceeds200Chars_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Name = new string('x', 201) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ─── Net / Gross ─────────────────────────────────────────────────────────

    [Fact]
    public void BothNetAndGross_Null_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Net = null, Gross = null };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Net_Negative_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Net = -1m };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Net");
    }

    [Fact]
    public void Gross_Zero_PassesValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Gross = 0m };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Gross");
    }

    // ─── Date ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Date_Null_PassesValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Date = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Date");
    }

    [Fact]
    public void Date_InFuture_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Date = DateTime.UtcNow.AddDays(5) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }

    // ─── ContractorId / Number / Description ─────────────────────────────────

    [Fact]
    public void ContractorId_EmptyGuid_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { ContractorId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContractorId");
    }

    [Fact]
    public void ContractorId_ValidGuid_PassesValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { ContractorId = Guid.NewGuid() };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "ContractorId");
    }

    [Fact]
    public void Number_Exceeds100Chars_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Number = new string('x', 101) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Number");
    }

    [Fact]
    public void Description_Exceeds2000Chars_FailsValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with { Description = new string('x', 2001) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    // ─── Document conflicts ───────────────────────────────────────────────────

    [Fact]
    public void BothDocumentAndUpdatedDocument_FailsValidation()
    {
        byte[] content = [0x01, 0x02];
        IFormFile doc = CreateFormFile(content, "a.pdf", "application/pdf");
        IFormFile updDoc = CreateFormFile(content, "b.pdf", "application/pdf");

        UpdateProjectCostCommand cmd = ValidCommand() with { Document = doc, UpdatedDocument = updDoc };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Document" &&
            e.ErrorMessage.Contains("Cannot provide both"));
    }

    [Fact]
    public void OnlyDocument_Valid_PassesConflictRule()
    {
        byte[] content = new byte[1024];
        IFormFile doc = CreateFormFile(content, "a.pdf", "application/pdf");

        UpdateProjectCostCommand cmd = ValidCommand() with { Document = doc, UpdatedDocument = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e =>
            e.PropertyName == "Document" && e.ErrorMessage.Contains("Cannot provide both"));
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public void AllFields_Valid_PassesValidation()
    {
        UpdateProjectCostCommand cmd = ValidCommand() with
        {
            Net = 80m,
            Gross = 100m,
            Date = DateTime.UtcNow.Date.AddDays(-1),
            ContractorId = Guid.NewGuid(),
            Number = "FV/2026/001",
            Description = "Opis"
        };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        Mock<IFormFile> mock = new();
        MemoryStream stream = new(content);
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(content.Length);
        mock.Setup(f => f.OpenReadStream()).Returns(stream);
        return mock.Object;
    }
}
