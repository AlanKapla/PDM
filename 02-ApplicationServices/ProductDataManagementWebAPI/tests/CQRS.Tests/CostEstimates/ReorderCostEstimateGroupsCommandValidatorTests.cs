using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.ReorderCostEstimateGroups;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class ReorderCostEstimateGroupsCommandValidatorTests
{
    private readonly ReorderCostEstimateGroupsCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public void Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public void Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Groups ===

    [Fact]
    public void Validate_WhenGroupsIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand() with { Groups = new List<ReorderGroupDto>() };

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Groups);
    }

    [Fact]
    public void Validate_WhenGroupsHasOneEntry_HasNoValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Groups);
    }

    // === Groups child rules — GroupId ===

    [Fact]
    public void Validate_WhenGroupEntryGroupIdIsEmpty_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand() with
        {
            Groups = [new ReorderGroupDto(Guid.Empty, null, 0)]
        };

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Groups[0].GroupId");
    }

    // === Groups child rules — Order ===

    [Fact]
    public void Validate_WhenGroupEntryOrderIsNegative_HasValidationError()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand() with
        {
            Groups = [new ReorderGroupDto(Guid.NewGuid(), null, -1)]
        };

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Groups[0].Order");
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ReorderCostEstimateGroupsCommand command = ValidCommand();

        // Act
        TestValidationResult<ReorderCostEstimateGroupsCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ReorderCostEstimateGroupsCommand ValidCommand() => new ReorderCostEstimateGroupsCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        Groups = [new ReorderGroupDto(Guid.NewGuid(), null, 0)]
    };
}
