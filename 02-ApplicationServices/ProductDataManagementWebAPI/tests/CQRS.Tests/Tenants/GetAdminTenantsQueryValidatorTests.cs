using Business.Interfaces.Model;
using CQRS.Tenants.GetAdminTenants;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Tenants;

public sealed class GetAdminTenantsQueryValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly GetAdminTenantsQueryValidator _validator;

    public GetAdminTenantsQueryValidatorTests()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        _validator = new GetAdminTenantsQueryValidator(_currentUserMock.Object);
    }

    // === IsAuthenticated ===

    [Fact]
    public void Validate_WhenUserIsNotAuthenticated_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        GetAdminTenantsQueryValidator validator = new(_currentUserMock.Object);
        GetAdminTenantsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetAdminTenantsQuery> result = validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === UserId ===

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.Id).Returns(Guid.Empty);
        GetAdminTenantsQueryValidator validator = new(_currentUserMock.Object);
        GetAdminTenantsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetAdminTenantsQuery> result = validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetAdminTenantsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetAdminTenantsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetAdminTenantsQuery ValidQuery() => new GetAdminTenantsQuery();
}
