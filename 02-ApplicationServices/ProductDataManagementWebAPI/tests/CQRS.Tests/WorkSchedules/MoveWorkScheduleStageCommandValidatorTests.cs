using CQRS.WorkSchedules.MoveWorkScheduleStage;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class MoveWorkScheduleStageCommandValidatorTests
{
    private readonly MoveWorkScheduleStageCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === ParentStageId ===

    [Fact]
    public void Validate_WhenParentStageIdIsNull_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand() with { ParentStageId = null };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ParentStageId);
    }

    [Fact]
    public void Validate_WhenParentStageIdEqualToWorkScheduleStageId_HasValidationError()
    {
        // Arrange
        Guid stageId = Guid.NewGuid();
        MoveWorkScheduleStageCommand command = ValidCommand() with
        {
            WorkScheduleStageId = stageId,
            ParentStageId = stageId
        };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("ParentStageId");
    }

    [Fact]
    public void Validate_WhenParentStageIdDiffersFromWorkScheduleStageId_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand() with { ParentStageId = Guid.NewGuid() };

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("ParentStageId");
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        MoveWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static MoveWorkScheduleStageCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid(),
        ParentStageId = null
    };
}
