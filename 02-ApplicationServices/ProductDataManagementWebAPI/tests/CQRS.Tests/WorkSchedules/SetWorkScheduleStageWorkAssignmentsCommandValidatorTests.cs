using CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkAssignmentsCommandValidatorTests
{
    private readonly SetWorkScheduleStageWorkAssignmentsCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === UserIds ===

    [Fact]
    public void Validate_WhenUserIdsIsNull_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with { UserIds = null! };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public void Validate_WhenUserIdsContainsDuplicates_HasValidationError()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with
        {
            UserIds = new List<Guid> { duplicateId, duplicateId }
        };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public void Validate_WhenUserIdsContainsEmptyGuid_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with
        {
            UserIds = new List<Guid> { Guid.Empty }
        };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUserIdsIsEmpty_HasNoValidationError()
    {
        // Arrange — empty list is valid (removing all assignments)
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand() with { UserIds = new List<Guid>() };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public void Validate_WhenUserIdsAreUnique_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserIds);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkAssignmentsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetWorkScheduleStageWorkAssignmentsCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        UserIds = new List<Guid> { Guid.NewGuid() }
    };
}
