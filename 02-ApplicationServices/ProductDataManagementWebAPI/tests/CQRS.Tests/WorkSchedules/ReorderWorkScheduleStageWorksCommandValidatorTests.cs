using CQRS.WorkSchedules.ReorderWorkScheduleStageWorks;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class ReorderWorkScheduleStageWorksCommandValidatorTests
{
    private readonly ReorderWorkScheduleStageWorksCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand() with { WorkScheduleStageId = Guid.Empty };

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageId);
    }

    // === OrderedWorkIds ===

    [Fact]
    public void Validate_WhenOrderedWorkIdsIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand() with { OrderedWorkIds = new List<Guid>() };

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderedWorkIds);
    }

    [Fact]
    public void Validate_WhenOrderedWorkIdsContainsDuplicates_HasValidationError()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        ReorderWorkScheduleStageWorksCommand command = ValidCommand() with
        {
            OrderedWorkIds = new List<Guid> { duplicateId, duplicateId }
        };

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderedWorkIds);
    }

    [Fact]
    public void Validate_WhenOrderedWorkIdsAreUnique_HasNoValidationError()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderedWorkIds);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ReorderWorkScheduleStageWorksCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderWorkScheduleStageWorksCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ReorderWorkScheduleStageWorksCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageId = Guid.NewGuid(),
        OrderedWorkIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
    };
}
