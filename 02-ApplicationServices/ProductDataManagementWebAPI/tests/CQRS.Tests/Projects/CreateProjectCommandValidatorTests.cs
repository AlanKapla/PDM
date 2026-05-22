using CQRS.Projects.CreateProject;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand() with { Name = new string('a', 201) };

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand() with { Name = new string('a', 200) };

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateProjectCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static CreateProjectCommand ValidCommand() => new CreateProjectCommand
    {
        TenantId = Guid.NewGuid(),
        Name = "Valid Project Name",
    };
}
