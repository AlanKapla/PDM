using System.Linq.Expressions;
using CQRS.WorkSchedules.CreateWorkSchedule;
using Entities.Models.CostEstimates;
using Entities.Models.WorkSchedules;
using FluentValidation.TestHelper;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.WorkSchedules;

public sealed class CreateWorkScheduleCommandValidatorTests
{
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly CreateWorkScheduleCommandValidator _validator;

    public CreateWorkScheduleCommandValidatorTests()
    {
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _workScheduleRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _validator = new CreateWorkScheduleCommandValidator(
            _costEstimateRepoMock.Object,
            _workScheduleRepoMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === Name ===

    [Fact]
    public async Task Validate_WhenNameIsEmpty_HasValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand() with { Name = string.Empty };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand() with { Name = new string('a', 256) };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameIsValid_HasNoValidationError()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // === CostEstimateId (conditional async rules) ===

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsNull_SkipsCostEstimateValidation()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand(); // CostEstimateId = null

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenCostEstimateNotFound_HasValidationError()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        CreateWorkScheduleCommand command = ValidCommand() with { CostEstimateId = Guid.NewGuid() };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenWorkScheduleAlreadyExistsForCostEstimate_HasValidationError()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _workScheduleRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        CreateWorkScheduleCommand command = ValidCommand() with { CostEstimateId = Guid.NewGuid() };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenCostEstimateFoundAndNoExistingWorkSchedule_HasNoValidationError()
    {
        // Arrange — defaults: CE found, WS not yet linked
        CreateWorkScheduleCommand command = ValidCommand() with { CostEstimateId = Guid.NewGuid() };

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand();

        // Act
        TestValidationResult<CreateWorkScheduleCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static CreateWorkScheduleCommand ValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Name = "Test Schedule"
    };
}
