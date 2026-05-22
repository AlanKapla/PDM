using CQRS.WorkSchedules.GetUserAssignedWorks;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetUserAssignedWorksQueryValidatorTests
{
    private readonly GetUserAssignedWorksQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetUserAssignedWorksQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetUserAssignedWorksQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetUserAssignedWorksQuery query = ValidQuery();

        // Act
        TestValidationResult<GetUserAssignedWorksQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetUserAssignedWorksQuery query = ValidQuery() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<GetUserAssignedWorksQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetUserAssignedWorksQuery query = ValidQuery();

        // Act
        TestValidationResult<GetUserAssignedWorksQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetUserAssignedWorksQuery query = ValidQuery();

        // Act
        TestValidationResult<GetUserAssignedWorksQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetUserAssignedWorksQuery ValidQuery() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid()
    };
}
