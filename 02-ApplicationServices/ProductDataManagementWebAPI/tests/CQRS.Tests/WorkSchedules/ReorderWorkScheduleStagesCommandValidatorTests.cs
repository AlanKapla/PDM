using CQRS.WorkSchedules.ReorderWorkScheduleStages;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class ReorderWorkScheduleStagesCommandValidatorTests
{
    private readonly ReorderWorkScheduleStagesCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === OrderedStageIds ===

    [Fact]
    public void Validate_WhenOrderedStageIdsIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand() with { OrderedStageIds = new List<Guid>() };

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderedStageIds);
    }

    [Fact]
    public void Validate_WhenOrderedStageIdsContainsDuplicates_HasValidationError()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        ReorderWorkScheduleStagesCommand command = ValidCommand() with
        {
            OrderedStageIds = new List<Guid> { duplicateId, duplicateId }
        };

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderedStageIds);
    }

    [Fact]
    public void Validate_WhenOrderedStageIdsAreUnique_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderedStageIds);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ReorderWorkScheduleStagesCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStagesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ReorderWorkScheduleStagesCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        OrderedStageIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
    };
}
