using CQRS.WorkSchedules.AddWorkScheduleStageWorkComment;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class AddWorkScheduleStageWorkCommentCommandValidatorTests
{
    private readonly AddWorkScheduleStageWorkCommentCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === WorkScheduleStageWorkId ===

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand() with { WorkScheduleStageWorkId = Guid.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleStageWorkIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleStageWorkId);
    }

    // === Content ===

    [Fact]
    public void Validate_WhenContentIsEmpty_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand() with { Content = string.Empty };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WhenContentExceedsMaxLength_HasValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand() with { Content = new string('a', 2001) };

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WhenContentIsValid_HasNoValidationError()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        AddWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<AddWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static AddWorkScheduleStageWorkCommentCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        WorkScheduleStageWorkId = Guid.NewGuid(),
        Content = "This is a comment."
    };
}
