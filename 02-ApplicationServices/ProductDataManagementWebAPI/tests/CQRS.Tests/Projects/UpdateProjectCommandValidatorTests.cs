using CQRS.Projects.UpdateProject;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand() with { Name = new string('a', 201) };

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand() with { Name = new string('a', 200) };

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateProjectCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateProjectCommand ValidCommand() => new UpdateProjectCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Name = "Valid Project Name",
    };
}
