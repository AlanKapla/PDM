using CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class DeleteWorkScheduleStageWorkCommentCommandValidatorTests
{
    private readonly DeleteWorkScheduleStageWorkCommentCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === CommentId ===

    [Fact]
    public void Validate_WhenCommentIdIsEmpty_HasValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand() with { CommentId = Guid.Empty };

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommentId);
    }

    [Fact]
    public void Validate_WhenCommentIdIsValid_HasNoValidationError()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CommentId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommentCommand command = ValidCommand();

        // Act
        TestValidationResult<DeleteWorkScheduleStageWorkCommentCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static DeleteWorkScheduleStageWorkCommentCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid(),
        CommentId = Guid.NewGuid()
    };
}
