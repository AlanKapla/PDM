using Business.Interfaces.Model;
using CQRS.Tenants.UpdateTenantMemberRole;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Tenants;

public sealed class UpdateTenantMemberRoleCommandValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateTenantMemberRoleCommandValidator _validator;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public UpdateTenantMemberRoleCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        _validator = new UpdateTenantMemberRoleCommandValidator(_currentUserMock.Object);
    }

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === UserId ===

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand() with { UserId = Guid.Empty };

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsCurrentUser_HasValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand() with { UserId = _currentUserId };

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsNotCurrentUser_HasNoValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    // === RoleId ===

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand() with { RoleId = Guid.Empty };

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public void Validate_WhenRoleIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateTenantMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateTenantMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateTenantMemberRoleCommand ValidCommand() => new UpdateTenantMemberRoleCommand
    {
        TenantId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        RoleId = Guid.NewGuid()
    };
}
