using CQRS.CostEstimates.MoveCostEstimateItem;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class MoveCostEstimateItemCommandValidatorTests
{
    private readonly MoveCostEstimateItemCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === ItemId ===

    [Fact]
    public void Validate_WhenItemIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand() with { ItemId = Guid.Empty };

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public void Validate_WhenItemIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ItemId);
    }

    // === TargetGroupId ===

    [Fact]
    public void Validate_WhenTargetGroupIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand() with { TargetGroupId = Guid.Empty };

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetGroupId);
    }

    [Fact]
    public void Validate_WhenTargetGroupIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TargetGroupId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        MoveCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static MoveCostEstimateItemCommand ValidCommand() => new MoveCostEstimateItemCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        ItemId = Guid.NewGuid(),
        TargetGroupId = Guid.NewGuid()
    };
}
