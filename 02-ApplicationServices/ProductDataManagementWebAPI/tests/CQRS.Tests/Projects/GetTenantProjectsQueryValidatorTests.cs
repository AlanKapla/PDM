using CQRS.Projects.GetTenantProjects;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class GetTenantProjectsQueryValidatorTests
{
    private readonly GetTenantProjectsQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetTenantProjectsQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetTenantProjectsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetTenantProjectsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetTenantProjectsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetTenantProjectsQuery query = ValidQuery();

        // Act
        TestValidationResult<GetTenantProjectsQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetTenantProjectsQuery ValidQuery() => new GetTenantProjectsQuery
    {
        TenantId = Guid.NewGuid(),
    };
}
