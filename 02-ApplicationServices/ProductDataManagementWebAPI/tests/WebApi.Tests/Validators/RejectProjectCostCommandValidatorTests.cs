using CQRS.ProjectCosts.RejectProjectCost;
using FluentAssertions;
using FluentValidation.Results;

namespace WebApi.Tests.Validators;

public sealed class RejectProjectCostCommandValidatorTests
{
    private readonly RejectProjectCostCommandValidator _sut = new();

    private static RejectProjectCostCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostId = Guid.NewGuid()
    };

    // ─── TenantId ────────────────────────────────────────────────────────────

    [Fact]
    public void TenantId_Empty_FailsValidation()
    {
        RejectProjectCostCommand cmd = ValidCommand() with { TenantId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ─── ProjectId ───────────────────────────────────────────────────────────

    [Fact]
    public void ProjectId_Empty_FailsValidation()
    {
        RejectProjectCostCommand cmd = ValidCommand() with { ProjectId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    // ─── CostId ──────────────────────────────────────────────────────────────

    [Fact]
    public void CostId_Empty_FailsValidation()
    {
        RejectProjectCostCommand cmd = ValidCommand() with { CostId = Guid.Empty };
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CostId");
    }

    // ─── Valid ────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidCommand_HasNoValidationErrors()
    {
        RejectProjectCostCommand cmd = ValidCommand();
        ValidationResult result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
