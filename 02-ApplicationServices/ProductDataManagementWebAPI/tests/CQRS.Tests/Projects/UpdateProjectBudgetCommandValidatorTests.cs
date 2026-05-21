using CQRS.Projects.UpdateProjectBudget;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class UpdateProjectBudgetCommandValidatorTests
{
    private readonly UpdateProjectBudgetCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === BudgetNet ===

    [Fact]
    public void Validate_WhenBudgetNetIsNegative_HasValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { BudgetNet = -1m };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BudgetNet);
    }

    [Fact]
    public void Validate_WhenBudgetNetIsZero_HasNoValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { BudgetNet = 0m };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BudgetNet);
    }

    [Fact]
    public void Validate_WhenBudgetNetIsNull_HasNoValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { BudgetNet = null };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BudgetNet);
    }

    // === BudgetGross ===

    [Fact]
    public void Validate_WhenBudgetGrossIsNegative_HasValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { BudgetGross = -1m };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BudgetGross);
    }

    [Fact]
    public void Validate_WhenBudgetGrossIsZero_HasNoValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { BudgetGross = 0m };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BudgetGross);
    }

    [Fact]
    public void Validate_WhenBudgetGrossIsNull_HasNoValidationError()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand() with { BudgetGross = null };

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BudgetGross);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateProjectBudgetCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectBudgetCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateProjectBudgetCommand ValidCommand() => new UpdateProjectBudgetCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        BudgetNet = 1000m,
        BudgetGross = 1230m,
    };
}
