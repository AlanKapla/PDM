using CQRS.CostEstimates.UpsertCostEstimateGroupField;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class UpsertCostEstimateGroupFieldCommandValidatorTests
{
    private readonly UpsertCostEstimateGroupFieldCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === GroupId ===

    [Fact]
    public void Validate_WhenGroupIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with { GroupId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupId);
    }

    [Fact]
    public void Validate_WhenGroupIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GroupId);
    }

    // === FieldDefinitionId — conditional (required only when FieldValueId is null) ===

    [Fact]
    public void Validate_WhenFieldValueIdIsNullAndFieldDefinitionIdIsEmpty_HasValidationError()
    {
        // Arrange — adding new field value: FieldValueId is null, so FieldDefinitionId is required
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with
        {
            FieldValueId = null,
            FieldDefinitionId = null
        };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    [Fact]
    public void Validate_WhenFieldValueIdIsNullAndFieldDefinitionIdIsProvided_HasNoValidationError()
    {
        // Arrange — adding new field value: FieldDefinitionId is provided
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with
        {
            FieldValueId = null,
            FieldDefinitionId = Guid.NewGuid()
        };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    [Fact]
    public void Validate_WhenFieldValueIdIsProvidedAndFieldDefinitionIdIsNull_HasNoValidationError()
    {
        // Arrange — updating existing field value: FieldDefinitionId is not required
        UpsertCostEstimateGroupFieldCommand command = ValidCommand() with
        {
            FieldValueId = Guid.NewGuid(),
            FieldDefinitionId = null
        };

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpsertCostEstimateGroupFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateGroupFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpsertCostEstimateGroupFieldCommand ValidCommand() => new UpsertCostEstimateGroupFieldCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        GroupId = Guid.NewGuid(),
        FieldValueId = null,
        FieldDefinitionId = Guid.NewGuid()
    };
}
