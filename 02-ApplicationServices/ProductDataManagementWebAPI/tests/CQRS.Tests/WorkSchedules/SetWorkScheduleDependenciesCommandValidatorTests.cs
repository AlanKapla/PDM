using CQRS.WorkSchedules.SetWorkScheduleDependencies;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleDependenciesCommandValidatorTests
{
    private readonly SetWorkScheduleDependenciesCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === Dependencies ===

    [Fact]
    public void Validate_WhenDependenciesIsNull_HasValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand() with { Dependencies = null! };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Dependencies);
    }

    [Fact]
    public void Validate_WhenDependenciesIsEmpty_HasNoValidationError()
    {
        // Arrange — clearing all dependencies is valid
        SetWorkScheduleDependenciesCommand command = ValidCommand() with { Dependencies = new List<WorkDependencyDto>() };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDependencyPredecessorWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand() with
        {
            Dependencies = new List<WorkDependencyDto>
            {
                new WorkDependencyDto(Guid.Empty, Guid.NewGuid(), WorkDependencyType.FinishToStart, 0)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDependencySuccessorWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand() with
        {
            Dependencies = new List<WorkDependencyDto>
            {
                new WorkDependencyDto(Guid.NewGuid(), Guid.Empty, WorkDependencyType.FinishToStart, 0)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPredecessorAndSuccessorAreTheSame_HasValidationError()
    {
        // Arrange
        Guid workId = Guid.NewGuid();
        SetWorkScheduleDependenciesCommand command = ValidCommand() with
        {
            Dependencies = new List<WorkDependencyDto>
            {
                new WorkDependencyDto(workId, workId, WorkDependencyType.FinishToStart, 0)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDependenciesContainsDuplicatePairs_HasValidationError()
    {
        // Arrange
        Guid pred = Guid.NewGuid();
        Guid succ = Guid.NewGuid();
        SetWorkScheduleDependenciesCommand command = ValidCommand() with
        {
            Dependencies = new List<WorkDependencyDto>
            {
                new WorkDependencyDto(pred, succ, WorkDependencyType.FinishToStart, 0),
                new WorkDependencyDto(pred, succ, WorkDependencyType.FinishToStart, 0)
            }
        };

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Dependencies);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetWorkScheduleDependenciesCommand command = ValidCommand();

        // Act
        TestValidationResult<SetWorkScheduleDependenciesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetWorkScheduleDependenciesCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        Dependencies = new List<WorkDependencyDto>
        {
            new WorkDependencyDto(Guid.NewGuid(), Guid.NewGuid(), WorkDependencyType.FinishToStart, 0)
        }
    };
}
