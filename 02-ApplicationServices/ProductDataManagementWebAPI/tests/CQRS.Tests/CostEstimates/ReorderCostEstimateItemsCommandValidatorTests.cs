using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.ReorderCostEstimateItems;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class ReorderCostEstimateItemsCommandValidatorTests
{
    private readonly ReorderCostEstimateItemsCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === GroupId ===

    [Fact]
    public void Validate_WhenGroupIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with { GroupId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupId);
    }

    [Fact]
    public void Validate_WhenGroupIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GroupId);
    }

    // === Items ===

    [Fact]
    public void Validate_WhenItemsIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with { Items = new List<ReorderItemDto>() };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WhenItemsHasOneEntry_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    // === Items child rules — ItemId ===

    [Fact]
    public void Validate_WhenItemEntryItemIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with
        {
            Items = [new ReorderItemDto(Guid.Empty, 0)]
        };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].ItemId");
    }

    // === Items child rules — Order ===

    [Fact]
    public void Validate_WhenItemEntryOrderIsNegative_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand() with
        {
            Items = [new ReorderItemDto(Guid.NewGuid(), -1)]
        };

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Order");
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ReorderCostEstimateItemsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateItemsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ReorderCostEstimateItemsCommand ValidCommand() => new ReorderCostEstimateItemsCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        GroupId = Guid.NewGuid(),
        Items = [new ReorderItemDto(Guid.NewGuid(), 0)]
    };
}
