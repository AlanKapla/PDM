using CQRS.CostEstimates.AddCostEstimateItem;
using Entities.Models.CostEstimates;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class AddCostEstimateItemCommandValidatorTests
{
    private readonly AddCostEstimateItemCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === GroupId ===

    [Fact]
    public void Validate_WhenGroupIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { GroupId = Guid.Empty };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupId);
    }

    [Fact]
    public void Validate_WhenGroupIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GroupId);
    }

    // === Order ===

    [Fact]
    public void Validate_WhenOrderIsNegative_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { Order = -1 };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public void Validate_WhenOrderIsZero_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { Order = 0 };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Order);
    }

    // === RelationType ===

    [Fact]
    public void Validate_WhenRelationTypeIsInvalid_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { RelationType = (ItemRelationType)999 };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RelationType);
    }

    [Fact]
    public void Validate_WhenRelationTypeIsNone_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with { RelationType = ItemRelationType.None };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RelationType);
    }

    // === ParentItemId — conditional rules ===

    [Fact]
    public void Validate_WhenRelationTypeIsOptionAndParentItemIdIsNull_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with
        {
            RelationType = ItemRelationType.Option,
            ParentItemId = null
        };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentItemId);
    }

    [Fact]
    public void Validate_WhenRelationTypeIsOptionAndParentItemIdIsProvided_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with
        {
            RelationType = ItemRelationType.Option,
            ParentItemId = Guid.NewGuid()
        };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ParentItemId);
    }

    [Fact]
    public void Validate_WhenRelationTypeIsNoneAndParentItemIdIsProvided_HasValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with
        {
            RelationType = ItemRelationType.None,
            ParentItemId = Guid.NewGuid()
        };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentItemId);
    }

    [Fact]
    public void Validate_WhenRelationTypeIsNoneAndParentItemIdIsNull_HasNoValidationError()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand() with
        {
            RelationType = ItemRelationType.None,
            ParentItemId = null
        };

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ParentItemId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        AddCostEstimateItemCommand command = ValidCommand();

        // Act
        TestValidationResult<AddCostEstimateItemCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static AddCostEstimateItemCommand ValidCommand() => new AddCostEstimateItemCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        GroupId = Guid.NewGuid(),
        RelationType = ItemRelationType.None,
        ParentItemId = null,
        Order = 0
    };
}
