using CQRS.CostEstimates.UpdateCostEstimate;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class UpdateCostEstimateCommandValidatorTests
{
    private readonly UpdateCostEstimateCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { Name = new string('a', 201) };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { Name = new string('a', 200) };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Description ===

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { Description = new string('a', 1001) };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand() with { Description = null };

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateCostEstimateCommand ValidCommand() => new UpdateCostEstimateCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        Name = "Updated Cost Estimate"
    };
}
