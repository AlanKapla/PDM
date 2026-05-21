using Business.Interfaces.Model;
using CQRS.CostEstimates.ShareCostEstimate;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentValidation.TestHelper;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimates;

public sealed class ShareCostEstimateCommandValidatorTests
{
    private readonly Mock<IReadRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly ShareCostEstimateCommandValidator _validator;

    public ShareCostEstimateCommandValidatorTests()
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

        _validator = new ShareCostEstimateCommandValidator(
            _costEstimateRepoMock.Object,
            _projectMemberRepoMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === ShareWithUserIds ===

    [Fact]
    public async Task Validate_WhenShareWithUserIdsIsEmpty_HasValidationError()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand() with { ShareWithUserIds = [] };

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShareWithUserIds);
    }

    [Fact]
    public async Task Validate_WhenShareWithUserIdsContainsDuplicates_HasValidationError()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        ShareCostEstimateCommand command = ValidCommand() with
        {
            ShareWithUserIds = [duplicateId, duplicateId]
        };

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShareWithUserIds);
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

        ShareCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

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

        ShareCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        ShareCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<ShareCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static ShareCostEstimateCommand ValidCommand() => new ShareCostEstimateCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        ShareWithUserIds = [Guid.NewGuid()]
    };
}
