using Business.Interfaces.Model;
using CQRS.Tenants.ActiveInvitations;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Tenants;

public sealed class ActiveTenantInvitationsQueryValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ActiveTenantInvitationsQueryValidator _validator;

    public ActiveTenantInvitationsQueryValidatorTests()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Email).Returns("user@example.com");

        _validator = new ActiveTenantInvitationsQueryValidator(_currentUserMock.Object);
    }

    // === IsAuthenticated ===

    [Fact]
    public void Validate_WhenUserIsNotAuthenticated_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        ActiveTenantInvitationsQueryValidator validator = new(_currentUserMock.Object);
        ActiveTenantInvitationsQuery query = ValidQuery();

        // Act
        TestValidationResult<ActiveTenantInvitationsQuery> result = validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUserIsAuthenticated_HasNoAuthValidationError()
    {
        // Arrange
        ActiveTenantInvitationsQuery query = ValidQuery();

        // Act
        TestValidationResult<ActiveTenantInvitationsQuery> result = _validator.TestValidate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    // === Email ===

    [Fact]
    public void Validate_WhenUserEmailIsEmpty_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.Email).Returns(string.Empty);
        ActiveTenantInvitationsQueryValidator validator = new(_currentUserMock.Object);
        ActiveTenantInvitationsQuery query = ValidQuery();

        // Act
        TestValidationResult<ActiveTenantInvitationsQuery> result = validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        ActiveTenantInvitationsQuery query = ValidQuery();

        // Act
        TestValidationResult<ActiveTenantInvitationsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ActiveTenantInvitationsQuery ValidQuery() => new ActiveTenantInvitationsQuery();
}
