using CQRS.CostEstimates.RecalculateCostEstimate;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class RecalculateCostEstimateCommandValidatorTests
{
    private readonly RecalculateCostEstimateCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        RecalculateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<RecalculateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static RecalculateCostEstimateCommand ValidCommand() => new RecalculateCostEstimateCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid()
    };
}
