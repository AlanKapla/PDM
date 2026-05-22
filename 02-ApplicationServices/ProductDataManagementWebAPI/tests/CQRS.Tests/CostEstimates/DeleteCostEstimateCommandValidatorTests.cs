using CQRS.CostEstimates.DeleteCostEstimate;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class DeleteCostEstimateCommandValidatorTests
{
    private readonly DeleteCostEstimateCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        DeleteCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static DeleteCostEstimateCommand ValidCommand() => new DeleteCostEstimateCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid()
    };
}
