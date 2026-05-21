using CQRS.Tenants.GetTenantMembers;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Tenants;

public sealed class GetTenantMembersQueryValidatorTests
{
    private readonly GetTenantMembersQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetTenantMembersQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetTenantMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetTenantMembersQuery query = ValidQuery();

        // Act
        TestValidationResult<GetTenantMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetTenantMembersQuery query = ValidQuery();

        // Act
        TestValidationResult<GetTenantMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetTenantMembersQuery ValidQuery() => new GetTenantMembersQuery
    {
        TenantId = Guid.NewGuid()
    };
}
