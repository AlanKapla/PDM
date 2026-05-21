using CQRS.CostEstimates.DeleteCostEstimateItem;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class DeleteCostEstimateItemCommandValidatorTests
{
    private readonly DeleteCostEstimateItemCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === ItemId ===

    [Fact]
    public void Validate_WhenItemIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand() with { ItemId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public void Validate_WhenItemIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ItemId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        DeleteCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static DeleteCostEstimateItemCommand ValidCommand() => new DeleteCostEstimateItemCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        ItemId = Guid.NewGuid()
    };
}
