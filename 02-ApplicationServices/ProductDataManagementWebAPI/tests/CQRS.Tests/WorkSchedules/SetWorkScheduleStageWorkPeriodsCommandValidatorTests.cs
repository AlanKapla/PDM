using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods;
using CQRS.WorkSchedules.Shared;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkPeriodsCommandValidatorTests
{
    private readonly SetWorkScheduleStageWorkPeriodsCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === Periods ===

    [Fact]
    public void Validate_WhenPeriodsIsNull_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with { Periods = null! };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Periods);
    }

    [Fact]
    public void Validate_WhenPeriodsIsEmpty_HasNoValidationError()
    {
        // Arrange — removing all periods is valid
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with { Periods = new List<WorkPeriodDto>() };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenPeriodEndDateNotGreaterThanStartDate_HasValidationError()
    {
        // Arrange
        DateTime start = new DateTime(2024, 1, 10);
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with
        {
            Periods = new List<WorkPeriodDto>
            {
                new WorkPeriodDto(start, start, false) // EndDate == StartDate
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPeriodEndDateBeforeStartDate_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with
        {
            Periods = new List<WorkPeriodDto>
            {
                new WorkPeriodDto(new DateTime(2024, 1, 10), new DateTime(2024, 1, 5), false)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPeriodsOverlap_HasValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with
        {
            Periods = new List<WorkPeriodDto>
            {
                new WorkPeriodDto(new DateTime(2024, 1, 1), new DateTime(2024, 1, 15), false),
                new WorkPeriodDto(new DateTime(2024, 1, 10), new DateTime(2024, 1, 20), false)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Periods);
    }

    [Fact]
    public void Validate_WhenPeriodsAreNonOverlapping_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand() with
        {
            Periods = new List<WorkPeriodDto>
            {
                new WorkPeriodDto(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10), false),
                new WorkPeriodDto(new DateTime(2024, 1, 11), new DateTime(2024, 1, 20), false)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Periods);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleStageWorkPeriodsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetWorkScheduleStageWorkPeriodsCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        Periods = new List<WorkPeriodDto>
        {
            new WorkPeriodDto(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), false)
        }
    };
}
