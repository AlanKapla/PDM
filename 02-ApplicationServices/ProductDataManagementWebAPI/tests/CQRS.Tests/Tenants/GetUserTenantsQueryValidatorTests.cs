using Business.Interfaces.Model;
using CQRS.Tenants.GetUserTenants;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Tenants;

public sealed class GetUserTenantsQueryValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly GetUserTenantsQueryValidator _validator;

    public GetUserTenantsQueryValidatorTests()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        _validator = new GetUserTenantsQueryValidator(_currentUserMock.Object);
    }

    // === IsAuthenticated ===

    [Fact]
    public void Validate_WhenUserIsNotAuthenticated_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        GetUserTenantsQueryValidator validator = new(_currentUserMock.Object);
        GetUserTenantsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetUserTenantsQuery> result = validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === UserId ===

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.Id).Returns(Guid.Empty);
        GetUserTenantsQueryValidator validator = new(_currentUserMock.Object);
        GetUserTenantsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetUserTenantsQuery> result = validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetUserTenantsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetUserTenantsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetUserTenantsQuery ValidQuery() => new GetUserTenantsQuery();
}
