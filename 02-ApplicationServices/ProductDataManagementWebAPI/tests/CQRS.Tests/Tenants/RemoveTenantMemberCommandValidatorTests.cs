using Business.Interfaces.Model;
using CQRS.Tenants.RemoveTenantMember;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Tenants;

public sealed class RemoveTenantMemberCommandValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly RemoveTenantMemberCommandValidator _validator;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public RemoveTenantMemberCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        _validator = new RemoveTenantMemberCommandValidator(_currentUserMock.Object);
    }

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveTenantMemberCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<RemoveTenantMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        RemoveTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === UserId ===

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveTenantMemberCommand command = ValidCommand() with { UserId = Guid.Empty };

        // Act
        TestValidationResult<RemoveTenantMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsCurrentUser_HasValidationError()
    {
        // Arrange
        RemoveTenantMemberCommand command = ValidCommand() with { UserId = _currentUserId };

        // Act
        TestValidationResult<RemoveTenantMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsNotCurrentUser_HasNoValidationError()
    {
        // Arrange
        RemoveTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        RemoveTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static RemoveTenantMemberCommand ValidCommand() => new RemoveTenantMemberCommand
    {
        TenantId = Guid.NewGuid(),
        UserId = Guid.NewGuid()
    };
}
