using Business.Interfaces.Constants;
using CQRS.CostEstimates.GetCostEstimates;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class GetCostEstimatesQueryValidatorTests
{
    private readonly GetCostEstimatesQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Scope ===

    [Fact]
    public void Validate_WhenScopeIsInvalidEnumValue_HasValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery() with { Scope = (ResourceScope)999 };

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scope);
    }

    [Fact]
    public void Validate_WhenScopeIsAll_HasNoValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery() with { Scope = ResourceScope.All };

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Scope);
    }

    [Fact]
    public void Validate_WhenScopeIsMine_HasNoValidationError()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery() with { Scope = ResourceScope.Mine };

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Scope);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetCostEstimatesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimatesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetCostEstimatesQuery ValidQuery() => new GetCostEstimatesQuery
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Scope = ResourceScope.Mine
    };
}
