using CQRS.Projects.SetProjectCurrency;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class SetProjectCurrencyCommandValidatorTests
{
    private readonly SetProjectCurrencyCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand();

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand();

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Code ===

    [Fact]
    public void Validate_WhenCodeIsEmpty_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Code = string.Empty };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeExceedsMaxLength_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Code = new string('A', 11) };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeContainsLowercaseLetters_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Code = "usd" };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeContainsOnlyOneLetter_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Code = "U" };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeIsValidUppercase_HasNoValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Code = "USD" };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Name = new string('a', 101) };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Name = new string('a', 100) };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Symbol ===

    [Fact]
    public void Validate_WhenSymbolIsNull_HasNoValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Symbol = null };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Symbol);
    }

    [Fact]
    public void Validate_WhenSymbolExceedsMaxLength_HasValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Symbol = new string('$', 11) };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Symbol);
    }

    [Fact]
    public void Validate_WhenSymbolIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand() with { Symbol = new string('$', 10) };

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Symbol);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetProjectCurrencyCommand command = ValidCommand();

        // Act
        TestValidationResult<SetProjectCurrencyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static SetProjectCurrencyCommand ValidCommand() => new SetProjectCurrencyCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Code = "USD",
        Name = "US Dollar",
        Symbol = "$",
    };
}
