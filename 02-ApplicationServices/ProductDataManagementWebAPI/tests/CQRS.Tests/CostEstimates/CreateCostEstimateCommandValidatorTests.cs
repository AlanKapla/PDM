using CQRS.CostEstimates.CreateCostEstimate;
using FluentValidation.TestHelper;

namespace CQRS.Tests.CostEstimates;

public sealed class CreateCostEstimateCommandValidatorTests
{
    private readonly CreateCostEstimateCommandValidator _validator = new();

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === TemplateId ===

    [Fact]
    public void Validate_WhenTemplateIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { TemplateId = Guid.Empty };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TemplateId);
    }

    [Fact]
    public void Validate_WhenTemplateIdIsValid_HasNoValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TemplateId);
    }

    // === Name ===

    [Fact]
    public void Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { Name = new string('a', 201) };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { Name = new string('a', 200) };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === Description ===

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_HasValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { Description = new string('a', 1001) };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_HasNoValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { Description = null };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand() with { Description = new string('a', 1000) };

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        CreateCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateCostEstimateCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static CreateCostEstimateCommand ValidCommand() => new CreateCostEstimateCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        TemplateId = Guid.NewGuid(),
        Name = "Valid Cost Estimate"
    };
}
