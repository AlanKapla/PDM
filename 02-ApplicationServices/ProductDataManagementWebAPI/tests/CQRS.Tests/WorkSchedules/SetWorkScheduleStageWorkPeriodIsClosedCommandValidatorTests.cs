using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkPeriodIsClosedCommandValidatorTests
{
    private readonly SetWorkScheduleStageWorkPeriodIsClosedCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === PeriodId ===

    [Fact]
    public void Validate_WhenPeriodIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand() with { PeriodId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PeriodId);
    }

    [Fact]
    public void Validate_WhenPeriodIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PeriodId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetWorkScheduleStageWorkPeriodIsClosedCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        PeriodId = Guid.NewGuid(),
        IsClosed = true
    };
}
