using CQRS.WorkSchedules.SyncWorkScheduleWithEstimate;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class SyncWorkScheduleWithEstimateCommandValidatorTests
{
    private readonly SyncWorkScheduleWithEstimateCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<SyncWorkScheduleWithEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SyncWorkScheduleWithEstimateCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid()
    };
}
