using CQRS.Tenants.ToggleTenantStatus;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Tenants;

public sealed class ToggleTenantStatusCommandValidatorTests
{
    private readonly ToggleTenantStatusCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ToggleTenantStatusCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ToggleTenantStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ToggleTenantStatusCommand command = ValidCommand();

        // Act
        TestValidationResult<ToggleTenantStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ToggleTenantStatusCommand command = ValidCommand();

        // Act
        TestValidationResult<ToggleTenantStatusCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ToggleTenantStatusCommand ValidCommand() => new ToggleTenantStatusCommand
    {
        TenantId = Guid.NewGuid(),
        IsActive = true
    };
}
