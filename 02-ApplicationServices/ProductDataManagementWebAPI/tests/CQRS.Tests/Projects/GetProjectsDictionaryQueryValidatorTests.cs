using CQRS.Projects.GetProjectsDictionary;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class GetProjectsDictionaryQueryValidatorTests
{
    private readonly GetProjectsDictionaryQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetProjectsDictionaryQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetProjectsDictionaryQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetProjectsDictionaryQuery query = ValidQuery();

        // Act
        TestValidationResult<GetProjectsDictionaryQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetProjectsDictionaryQuery query = ValidQuery();

        // Act
        TestValidationResult<GetProjectsDictionaryQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetProjectsDictionaryQuery ValidQuery() => new GetProjectsDictionaryQuery
    {
        TenantId = Guid.NewGuid(),
    };
}
