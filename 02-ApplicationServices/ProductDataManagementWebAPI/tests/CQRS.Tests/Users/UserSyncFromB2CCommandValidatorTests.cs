using CQRS.Users.UserSyncFromB2C;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Users;

public sealed class UserSyncFromB2CCommandValidatorTests
{
    private readonly UserSyncFromB2CCommandValidator _validator = new();

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UserSyncFromB2CCommand command = new UserSyncFromB2CCommand();

        // Act
        TestValidationResult<UserSyncFromB2CCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
