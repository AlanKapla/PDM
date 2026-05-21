using CQRS.CostEstimates.DeleteCostEstimateGroup;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class DeleteCostEstimateGroupCommandValidatorTests
{
    private readonly DeleteCostEstimateGroupCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === GroupId ===

    [Fact]
    public void Validate_WhenGroupIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand() with { GroupId = Guid.Empty };

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupId);
    }

    [Fact]
    public void Validate_WhenGroupIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GroupId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        DeleteCostEstimateGroupCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteCostEstimateGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static DeleteCostEstimateGroupCommand ValidCommand() => new DeleteCostEstimateGroupCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        GroupId = Guid.NewGuid()
    };
}
