using CQRS.WorkSchedules.GetWorkSchedule;
using FluentValidation.TestHelper;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetWorkScheduleQueryValidatorTests
{
    private readonly GetWorkScheduleQueryValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === WorkScheduleId ===

    [Fact]
    public void Validate_WhenWorkScheduleIdIsEmpty_HasValidationError()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery() with { WorkScheduleId = Guid.Empty };

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    [Fact]
    public void Validate_WhenWorkScheduleIdIsValid_HasNoValidationError()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WorkScheduleId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery();

        // Act
        TestValidationResult<GetWorkScheduleQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static GetWorkScheduleQuery ValidQuery() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        WorkScheduleId = Guid.NewGuid()
    };
}
