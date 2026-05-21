using CQRS.WorkSchedules.DeleteWorkScheduleStage;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class DeleteWorkScheduleStageCommandValidatorTests
{
    private readonly DeleteWorkScheduleStageCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static DeleteWorkScheduleStageCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid()
    };
}
