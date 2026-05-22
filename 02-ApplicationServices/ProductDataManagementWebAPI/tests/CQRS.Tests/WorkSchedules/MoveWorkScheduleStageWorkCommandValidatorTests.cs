using CQRS.WorkSchedules.MoveWorkScheduleStageWork;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class MoveWorkScheduleStageWorkCommandValidatorTests
{
    private readonly MoveWorkScheduleStageWorkCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === TargetStageId ===

    [Fact]
    public void Validate_WhenTargetStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { TargetStageId = Guid.Empty };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetStageId);
    }

    [Fact]
    public void Validate_WhenTargetStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TargetStageId);
    }

    // === TargetOrder ===

    [Fact]
    public void Validate_WhenTargetOrderIsNegative_HasValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { TargetOrder = -1 };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetOrder);
    }

    [Fact]
    public void Validate_WhenTargetOrderIsZero_HasNoValidationError()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand() with { TargetOrder = 0 };

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TargetOrder);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        MoveWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<MoveWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static MoveWorkScheduleStageWorkCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        TargetStageId = Guid.NewGuid(),
        TargetOrder = 0
    };
}
