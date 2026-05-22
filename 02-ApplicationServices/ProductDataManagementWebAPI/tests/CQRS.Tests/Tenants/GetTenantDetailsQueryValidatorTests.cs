using CQRS.Tenants.GetTenantDetails;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Tenants;

public sealed class GetTenantDetailsQueryValidatorTests
{
    private readonly GetTenantDetailsQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetTenantDetailsQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetTenantDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetTenantDetailsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetTenantDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetTenantDetailsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetTenantDetailsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetTenantDetailsQuery ValidQuery() => new GetTenantDetailsQuery
    {
        TenantId = Guid.NewGuid()
    };
}
