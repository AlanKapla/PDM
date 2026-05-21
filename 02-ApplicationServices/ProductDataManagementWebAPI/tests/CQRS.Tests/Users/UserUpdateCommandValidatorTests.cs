using Business.Interfaces.Model;
using CQRS.Users.UserUpdate;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Users;

public sealed class UserUpdateCommandValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UserUpdateCommandValidator _validator;

    public UserUpdateCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        _validator = new UserUpdateCommandValidator(_currentUserMock.Object);
    }

    // === IsAuthenticated ===

    [Fact]
    public void Validate_WhenUserIsNotAuthenticated_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        UserUpdateCommandValidator validator = new(_currentUserMock.Object);
        UserUpdateCommand command = ValidCommand();

        // Act
        TestValidationResult<UserUpdateCommand> result = validator.TestValidate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === FirstName ===

    [Fact]
    public void Validate_WhenFirstNameIsNull_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { FirstName = null! };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_WhenFirstNameIsEmpty_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { FirstName = string.Empty };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_WhenFirstNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { FirstName = new string('a', 101) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_WhenFirstNameIsValid_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand();

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    // === LastName ===

    [Fact]
    public void Validate_WhenLastNameIsNull_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { LastName = null! };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_WhenLastNameIsEmpty_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { LastName = string.Empty };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_WhenLastNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { LastName = new string('a', 101) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_WhenLastNameIsValid_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand();

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    // === PhoneNumber (optional, When not null) ===

    [Fact]
    public void Validate_WhenPhoneNumberExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { PhoneNumber = new string('1', 21) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WhenPhoneNumberIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { PhoneNumber = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    // === CompanyName (optional, When not null) ===

    [Fact]
    public void Validate_WhenCompanyNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { CompanyName = new string('a', 201) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Validate_WhenCompanyNameIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { CompanyName = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CompanyName);
    }

    // === TaxId (optional, When not null) ===

    [Fact]
    public void Validate_WhenTaxIdExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { TaxId = new string('1', 51) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaxId);
    }

    [Fact]
    public void Validate_WhenTaxIdIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { TaxId = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TaxId);
    }

    // === Street (optional, When not null) ===

    [Fact]
    public void Validate_WhenStreetExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { Street = new string('a', 201) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Street);
    }

    [Fact]
    public void Validate_WhenStreetIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { Street = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Street);
    }

    // === City (optional, When not null) ===

    [Fact]
    public void Validate_WhenCityExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { City = new string('a', 101) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Validate_WhenCityIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { City = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.City);
    }

    // === PostalCode (optional, When not null) ===

    [Fact]
    public void Validate_WhenPostalCodeExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { PostalCode = new string('1', 21) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PostalCode);
    }

    [Fact]
    public void Validate_WhenPostalCodeIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { PostalCode = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PostalCode);
    }

    // === Country (optional, When not null) ===

    [Fact]
    public void Validate_WhenCountryExceedsMaxLength_HasValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { Country = new string('a', 101) };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public void Validate_WhenCountryIsNull_HasNoValidationError()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand() with { Country = null };

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Country);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UserUpdateCommand command = ValidCommand();

        // Act
        TestValidationResult<UserUpdateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UserUpdateCommand ValidCommand() => new UserUpdateCommand(
        FirstName: "John",
        LastName: "Doe",
        PhoneNumber: null,
        CompanyName: null,
        TaxId: null,
        Street: null,
        City: null,
        PostalCode: null,
        Country: null
    );
}
