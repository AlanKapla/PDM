using CQRS.WorkSchedules.AddWorkScheduleStage;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class AddWorkScheduleStageCommandValidatorTests
{
    private readonly AddWorkScheduleStageCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { Name = new string('a', 256) };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Order ===

    [Fact]
    public void Validate_WhenOrderIsNegative_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { Order = -1 };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public void Validate_WhenOrderIsZero_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { Order = 0 };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public void Validate_WhenOrderIsPositive_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand() with { Order = 5 };

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Order);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static AddWorkScheduleStageCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        Name = "Stage One",
        Order = 0
    };
}
