using CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class UpdateWorkScheduleStageWorkCommentCommandValidatorTests
{
    private readonly UpdateWorkScheduleStageWorkCommentCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === CommentId ===

    [Fact]
    public void Validate_WhenCommentIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand() with { CommentId = Guid.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommentId);
    }

    [Fact]
    public void Validate_WhenCommentIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CommentId);
    }

    // === Content ===

    [Fact]
    public void Validate_WhenContentIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand() with { Content = string.Empty };

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WhenContentExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand() with { Content = new string('a', 2001) };

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WhenContentIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateWorkScheduleStageWorkCommentCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        CommentId = Guid.NewGuid(),
        Content = "Updated comment text."
    };
}
