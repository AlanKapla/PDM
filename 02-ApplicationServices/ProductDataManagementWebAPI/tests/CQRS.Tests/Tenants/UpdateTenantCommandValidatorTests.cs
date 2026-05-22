using CQRS.Tenants.UpdateTenant;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Tenants;

public sealed class UpdateTenantCommandValidatorTests
{
    private readonly UpdateTenantCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateTenantCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateTenantCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<UpdateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UpdateTenantCommand command = ValidCommand() with { Name = new string('a', 201) };

        // Act
        TestValidationResult<UpdateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        UpdateTenantCommand command = ValidCommand() with { Name = new string('a', 200) };

        // Act
        TestValidationResult<UpdateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateTenantCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateTenantCommand ValidCommand() => new UpdateTenantCommand
    {
        TenantId = Guid.NewGuid(),
        Name = "Valid Tenant Name"
    };
}
