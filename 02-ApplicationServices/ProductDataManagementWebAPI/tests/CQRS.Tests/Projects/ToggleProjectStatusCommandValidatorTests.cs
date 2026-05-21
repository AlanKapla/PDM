using CQRS.Projects.ToggleProjectStatus;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class ToggleProjectStatusCommandValidatorTests
{
    private readonly ToggleProjectStatusCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ToggleProjectStatusCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ToggleProjectStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ToggleProjectStatusCommand command = ValidCommand();

        // Act
        TestValidationResult<ToggleProjectStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        ToggleProjectStatusCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<ToggleProjectStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        ToggleProjectStatusCommand command = ValidCommand();

        // Act
        TestValidationResult<ToggleProjectStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ToggleProjectStatusCommand command = ValidCommand();

        // Act
        TestValidationResult<ToggleProjectStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ToggleProjectStatusCommand ValidCommand() => new ToggleProjectStatusCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        IsActive = true,
    };
}
