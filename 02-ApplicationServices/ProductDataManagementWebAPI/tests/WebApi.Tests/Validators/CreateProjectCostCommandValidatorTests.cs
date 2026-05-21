using CQRS.ProjectCosts.CreateProjectCost;
using FluentAssertions;
using FluentValidation.Results;

namespace WebApi.Tests.Validators;

public class CreateProjectCostCommandValidatorTests
{
    private readonly CreateProjectCostCommandValidator _sut = new();

    private static CreateProjectCostCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Name = "Koszt budowlany",
        Gross = 100m
    };

    // ─── TenantId ────────────────────────────────────────────────────────────

    [Fact]
    public void TenantId_Empty_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { TenantId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ─── ProjectId ───────────────────────────────────────────────────────────

    [Fact]
    public void ProjectId_Empty_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { ProjectId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    // ─── Name ────────────────────────────────────────────────────────────────

    [Fact]
    public void Name_Empty_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Name = "" };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Exceeds200Chars_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Name = new string('x', 201) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Exactly200Chars_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Name = new string('x', 200) };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Name");
    }

    // ─── Net / Gross ─────────────────────────────────────────────────────────

    [Fact]
    public void BothNetAndGross_Null_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Net = null, Gross = null };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void OnlyNet_Provided_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Net = 50m, Gross = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Net_Negative_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Net = -1m };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Net");
    }

    [Fact]
    public void Gross_Negative_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Gross = -0.01m };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Gross");
    }

    [Fact]
    public void Net_Zero_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Net = 0m };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Net");
    }

    // ─── Date ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Date_Null_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Date = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Date");
    }

    [Fact]
    public void Date_InFuture_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Date = DateTime.UtcNow.AddDays(5) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }

    [Fact]
    public void Date_Today_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Date = DateTime.UtcNow.Date };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Date");
    }

    // ─── ContractorId ─────────────────────────────────────────────────────────

    [Fact]
    public void ContractorId_Null_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { ContractorId = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "ContractorId");
    }

    [Fact]
    public void ContractorId_EmptyGuid_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { ContractorId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContractorId");
    }

    [Fact]
    public void ContractorId_ValidGuid_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { ContractorId = Guid.NewGuid() };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "ContractorId");
    }

    // ─── Number ──────────────────────────────────────────────────────────────

    [Fact]
    public void Number_Null_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Number = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Number");
    }

    [Fact]
    public void Number_Exceeds100Chars_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Number = new string('x', 101) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Number");
    }

    // ─── Description ─────────────────────────────────────────────────────────

    [Fact]
    public void Description_Exceeds2000Chars_FailsValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Description = new string('x', 2001) };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Null_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with { Description = null };
        ValidationResult result = _sut.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Description");
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public void AllRequiredFields_Valid_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand();
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AllOptionalFields_Filled_PassesValidation()
    {
        CreateProjectCostCommand cmd = ValidCommand() with
        {
            Net = 80m,
            Gross = 100m,
            Date = DateTime.UtcNow.Date.AddDays(-1),
            ContractorId = Guid.NewGuid(),
            Number = "FV/2026/001",
            Description = "Opis kosztu"
        };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
