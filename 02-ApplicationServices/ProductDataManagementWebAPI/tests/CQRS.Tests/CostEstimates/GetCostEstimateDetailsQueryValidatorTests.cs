using CQRS.CostEstimates.GetCostEstimateDetails;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class GetCostEstimateDetailsQueryValidatorTests
{
    private readonly GetCostEstimateDetailsQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetCostEstimateDetailsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetCostEstimateDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetCostEstimateDetailsQuery ValidQuery() => new GetCostEstimateDetailsQuery
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid()
    };
}
