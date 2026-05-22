using CQRS.CostEstimates.AddCostEstimateGroup;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class AddCostEstimateGroupCommandValidatorTests
{
    private readonly AddCostEstimateGroupCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Order ===

    [Fact]
    public void Validate_WhenOrderIsNegative_HasValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand() with { Order = -1 };

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public void Validate_WhenOrderIsZero_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand() with { Order = 0 };

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public void Validate_WhenOrderIsPositive_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand() with { Order = 5 };

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Order);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        AddCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static AddCostEstimateGroupCommand ValidCommand() => new AddCostEstimateGroupCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        Order = 0
    };
}
