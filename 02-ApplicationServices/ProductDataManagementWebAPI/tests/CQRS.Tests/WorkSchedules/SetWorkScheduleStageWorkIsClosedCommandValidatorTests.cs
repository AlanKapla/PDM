using CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkIsClosedCommandValidatorTests
{
    private readonly SetWorkScheduleStageWorkIsClosedCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkIsClosedCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetWorkScheduleStageWorkIsClosedCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        IsClosed = false
    };
}
