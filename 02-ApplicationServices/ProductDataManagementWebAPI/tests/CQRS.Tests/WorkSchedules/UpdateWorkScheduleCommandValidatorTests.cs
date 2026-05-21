using CQRS.WorkSchedules.UpdateWorkSchedule;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class UpdateWorkScheduleCommandValidatorTests
{
    private readonly UpdateWorkScheduleCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand() with { Name = new string('a', 256) };

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateWorkScheduleCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        Name = "Updated Schedule"
    };
}
