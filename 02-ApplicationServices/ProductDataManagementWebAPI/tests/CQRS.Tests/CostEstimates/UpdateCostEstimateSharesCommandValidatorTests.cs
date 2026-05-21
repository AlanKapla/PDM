using CQRS.CostEstimates.UpdateCostEstimateShares;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentValidation.TestHelper;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimates;

public sealed class UpdateCostEstimateSharesCommandValidatorTests
{
    private readonly Mock<IReadRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly UpdateCostEstimateSharesCommandValidator _validator;

    public UpdateCostEstimateSharesCommandValidatorTests()
    {
        // Default: cost estimate exists
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default: all users are project members (CountAsync returns matching count)
        _projectMemberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _validator = new UpdateCostEstimateSharesCommandValidator(
            _costEstimateRepoMock.Object,
            _projectMemberRepoMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === UserIds — UniqueIds when count > 0 ===

    [Fact]
    public async Task Validate_WhenUserIdsContainsDuplicates_HasValidationError()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        UpdateCostEstimateSharesCommand command = ValidCommand() with
        {
            UserIds = [duplicateId, duplicateId]
        };

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public async Task Validate_WhenUserIdsIsEmpty_HasNoValidationError()
    {
        // Arrange — empty list is valid (removes all shares)
        UpdateCostEstimateSharesCommand command = ValidCommand() with { UserIds = [] };

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserIds);
    }

    // === Async — CostEstimate does not exist ===

    [Fact]
    public async Task Validate_WhenCostEstimateDoesNotExist_HasValidationError()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UpdateCostEstimateSharesCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === Async — Users are not project members ===

    [Fact]
    public async Task Validate_WhenUsersAreNotProjectMembers_HasValidationError()
    {
        // Arrange
        _projectMemberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        UpdateCostEstimateSharesCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateCostEstimateSharesCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateCostEstimateSharesCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static UpdateCostEstimateSharesCommand ValidCommand() => new UpdateCostEstimateSharesCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        UserIds = [Guid.NewGuid()]
    };
}
