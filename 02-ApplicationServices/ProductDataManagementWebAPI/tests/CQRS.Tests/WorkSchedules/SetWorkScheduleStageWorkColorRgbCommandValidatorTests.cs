using CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkColorRgbCommandValidatorTests
{
    private readonly SetWorkScheduleStageWorkColorRgbCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === ColorRgb ===

    [Fact]
    public void Validate_WhenColorRgbIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { ColorRgb = string.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorRgb);
    }

    [Fact]
    public void Validate_WhenColorRgbExceedsMaxLength_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { ColorRgb = new string('a', 21) };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorRgb);
    }

    [Fact]
    public void Validate_WhenColorRgbIsInvalidFormat_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { ColorRgb = "not-a-color" };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorRgb);
    }

    [Fact]
    public void Validate_WhenColorRgbIsValidHex_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand() with { ColorRgb = "#AABBCC" };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ColorRgb);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkColorRgbCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetWorkScheduleStageWorkColorRgbCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        ColorRgb = "#1A2B3C"
    };
}
