using CQRS.WorkSchedules.AddWorkScheduleStageWork;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class AddWorkScheduleStageWorkCommandValidatorTests
{
    private readonly AddWorkScheduleStageWorkCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { Name = new string('a', 256) };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Order ===

    [Fact]
    public void Validate_WhenOrderIsNegative_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { Order = -1 };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public void Validate_WhenOrderIsZero_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { Order = 0 };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Order);
    }

    // === ColorRgb ===

    [Fact]
    public void Validate_WhenColorRgbIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { ColorRgb = string.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorRgb);
    }

    [Fact]
    public void Validate_WhenColorRgbIsInvalidFormat_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { ColorRgb = "not-a-color" };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorRgb);
    }

    [Fact]
    public void Validate_WhenColorRgbIsValidHex_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand() with { ColorRgb = "#FF5733" };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ColorRgb);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static AddWorkScheduleStageWorkCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid(),
        Name = "Work Item",
        Order = 0,
        ColorRgb = "#1A2B3C"
    };
}
