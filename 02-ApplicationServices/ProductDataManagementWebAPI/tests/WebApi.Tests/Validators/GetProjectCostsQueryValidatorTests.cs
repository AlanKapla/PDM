using Business.Interfaces.Constants;
using CQRS.ProjectCosts.GetProjectCosts;
using FluentAssertions;
using FluentValidation.Results;

namespace WebApi.Tests.Validators;

public sealed class GetProjectCostsQueryValidatorTests
{
    private readonly GetProjectCostsQueryValidator _sut = new();

    private static GetProjectCostsQuery ValidQuery() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Scope = ResourceScope.All
    };

    // ─── TenantId ────────────────────────────────────────────────────────────

    [Fact]
    public void TenantId_Empty_FailsValidation()
    {
        GetProjectCostsQuery query = ValidQuery() with { TenantId = Guid.Empty };
        ValidationResult result = _sut.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ─── ProjectId ───────────────────────────────────────────────────────────

    [Fact]
    public void ProjectId_Empty_FailsValidation()
    {
        GetProjectCostsQuery query = ValidQuery() with { ProjectId = Guid.Empty };
        ValidationResult result = _sut.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    // ─── Scope ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scope_InvalidValue_FailsValidation()
    {
        GetProjectCostsQuery query = ValidQuery() with { Scope = (ResourceScope)99 };
        ValidationResult result = _sut.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Scope");
    }

    // ─── Valid ────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidQuery_HasNoValidationErrors()
    {
        GetProjectCostsQuery query = ValidQuery();
        ValidationResult result = _sut.Validate(query);
        result.IsValid.Should().BeTrue();
    }
}
