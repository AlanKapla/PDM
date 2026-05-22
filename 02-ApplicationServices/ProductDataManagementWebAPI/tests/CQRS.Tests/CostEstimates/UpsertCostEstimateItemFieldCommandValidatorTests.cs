using CQRS.CostEstimates.UpsertCostEstimateItemField;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class UpsertCostEstimateItemFieldCommandValidatorTests
{
    private readonly UpsertCostEstimateItemFieldCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === ItemId ===

    [Fact]
    public void Validate_WhenItemIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with { ItemId = Guid.Empty };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public void Validate_WhenItemIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ItemId);
    }

    // === FieldDefinitionId — conditional (required only when FieldValueId is null) ===

    [Fact]
    public void Validate_WhenFieldValueIdIsNullAndFieldDefinitionIdIsEmpty_HasValidationError()
    {
        // Arrange — adding new field value: FieldValueId is null, so FieldDefinitionId is required
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with
        {
            FieldValueId = null,
            FieldDefinitionId = null
        };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    [Fact]
    public void Validate_WhenFieldValueIdIsNullAndFieldDefinitionIdIsProvided_HasNoValidationError()
    {
        // Arrange — adding new field value: FieldDefinitionId is provided
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with
        {
            FieldValueId = null,
            FieldDefinitionId = Guid.NewGuid()
        };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    [Fact]
    public void Validate_WhenFieldValueIdIsProvidedAndFieldDefinitionIdIsNull_HasNoValidationError()
    {
        // Arrange — updating existing field value: FieldDefinitionId is not required
        UpsertCostEstimateItemFieldCommand command = ValidCommand() with
        {
            FieldValueId = Guid.NewGuid(),
            FieldDefinitionId = null
        };

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FieldDefinitionId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpsertCostEstimateItemFieldCommand command = ValidCommand();

        // Act
        TestValidationResult<UpsertCostEstimateItemFieldCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpsertCostEstimateItemFieldCommand ValidCommand() => new UpsertCostEstimateItemFieldCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        ItemId = Guid.NewGuid(),
        FieldValueId = null,
        FieldDefinitionId = Guid.NewGuid()
    };
}
