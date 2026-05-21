using CQRS.Tenants.CreateTenant;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Tenants;

public sealed class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        CreateTenantCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<CreateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        CreateTenantCommand command = ValidCommand() with { Name = new string('a', 201) };

        // Act
        TestValidationResult<CreateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        CreateTenantCommand command = ValidCommand() with { Name = new string('a', 200) };

        // Act
        TestValidationResult<CreateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        CreateTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static CreateTenantCommand ValidCommand() => new CreateTenantCommand
    {
        Name = "Valid Tenant Name"
    };
}
