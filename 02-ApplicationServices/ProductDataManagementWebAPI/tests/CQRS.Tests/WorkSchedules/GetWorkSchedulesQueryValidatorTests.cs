using Business.Interfaces.Constants;
using CQRS.WorkSchedules.GetWorkSchedules;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetWorkSchedulesQueryValidatorTests
{
    private readonly GetWorkSchedulesQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Scope ===

    [Fact]
    public void Validate_WhenScopeIsInvalidEnumValue_HasValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery() with { Scope = (ResourceScope)999 };

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scope);
    }

    [Fact]
    public void Validate_WhenScopeIsAll_HasNoValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery() with { Scope = ResourceScope.All };

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Scope);
    }

    [Fact]
    public void Validate_WhenScopeIsMine_HasNoValidationError()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery() with { Scope = ResourceScope.Mine };

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Scope);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkSchedulesQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetWorkSchedulesQuery ValidQuery() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Scope = ResourceScope.All
    };
}
