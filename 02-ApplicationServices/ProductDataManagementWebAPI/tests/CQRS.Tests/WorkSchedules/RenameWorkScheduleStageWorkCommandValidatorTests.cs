using CQRS.WorkSchedules.RenameWorkScheduleStageWork;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class RenameWorkScheduleStageWorkCommandValidatorTests
{
    private readonly RenameWorkScheduleStageWorkCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand() with { Name = new string('a', 256) };

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsValid_HasNoValidationError()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        // Act
        TestValidationResult<RenameWorkScheduleStageWorkCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static RenameWorkScheduleStageWorkCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        Name = "Renamed Work"
    };
}
