using CQRS.WorkSchedules.DeleteWorkScheduleStageWork;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class DeleteWorkScheduleStageWorkCommandValidatorTests
{
    private readonly DeleteWorkScheduleStageWorkCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static DeleteWorkScheduleStageWorkCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid()
    };
}
