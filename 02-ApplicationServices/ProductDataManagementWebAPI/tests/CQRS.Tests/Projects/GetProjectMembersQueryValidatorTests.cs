using CQRS.Projects.GetProjectMembers;
using FluentValidation.TestHelper;

namespace CQRS.Tests.Projects;

public sealed class GetProjectMembersQueryValidatorTests
{
    private readonly GetProjectMembersQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetProjectMembersQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetProjectMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetProjectMembersQuery query = ValidQuery();

        // Act
        TestValidationResult<GetProjectMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetProjectMembersQuery query = ValidQuery() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<GetProjectMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetProjectMembersQuery query = ValidQuery();

        // Act
        TestValidationResult<GetProjectMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetProjectMembersQuery query = ValidQuery();

        // Act
        TestValidationResult<GetProjectMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetProjectMembersQuery ValidQuery() => new GetProjectMembersQuery
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
    };
}
