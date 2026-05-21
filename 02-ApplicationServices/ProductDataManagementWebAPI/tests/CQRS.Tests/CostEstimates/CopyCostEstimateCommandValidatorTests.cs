using Business.Interfaces.Model;
using CQRS.CostEstimates.CopyCostEstimate;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using FluentValidation.TestHelper;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.CostEstimates;

public sealed class CopyCostEstimateCommandValidatorTests
{
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CopyCostEstimateCommandValidator _validator;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _targetProjectId = Guid.NewGuid();

    public CopyCostEstimateCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        // Default: tenant admin — bypasses membership checks
        TenantCtxSnapshot tenantSnapshot = new TenantCtxSnapshot(
            TenantId: Guid.NewGuid(),
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: [],
            IsTenantAdmin: true,
            IsActive: true);

        _currentUserMock
            .Setup(u => u.GetActiveTenantSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantSnapshot);

        // Default: cost estimate found (owned by current user)
        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(new CostEstimate());

        // Default: target projects found
        _projectRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(new List<Project> { new Project { Id = _targetProjectId } });

        // Default: user is member of target projects
        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<ProjectMember, object>>[]>()))
            .ReturnsAsync(new List<ProjectMember>());

        _validator = new CopyCostEstimateCommandValidator(
            _costEstimateRepoMock.Object,
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _currentUserMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === CostEstimateId ===

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsEmpty_HasValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand() with { CostEstimateId = Guid.Empty };

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostEstimateId);
    }

    [Fact]
    public async Task Validate_WhenCostEstimateIdIsValid_HasNoValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostEstimateId);
    }

    // === TargetProjectIds ===

    [Fact]
    public async Task Validate_WhenTargetProjectIdsIsEmpty_HasValidationError()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand() with { TargetProjectIds = [] };

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetProjectIds);
    }

    [Fact]
    public async Task Validate_WhenTargetProjectIdsContainsDuplicates_HasValidationError()
    {
        // Arrange
        Guid duplicateId = Guid.NewGuid();
        CopyCostEstimateCommand command = ValidCommand() with
        {
            TargetProjectIds = [duplicateId, duplicateId]
        };

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetProjectIds);
    }

    // === Source project cannot be in TargetProjectIds ===

    [Fact]
    public async Task Validate_WhenTargetProjectIdsContainsSourceProjectId_HasValidationError()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        CopyCostEstimateCommand command = ValidCommand() with
        {
            ProjectId = projectId,
            TargetProjectIds = [projectId]
        };

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Async — CostEstimate not found or not owned by user ===

    [Fact]
    public async Task Validate_WhenCostEstimateNotFoundForUser_HasValidationError()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync((CostEstimate?)null);

        CopyCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Async — Target projects not found ===

    [Fact]
    public async Task Validate_WhenTargetProjectsNotFound_HasValidationError()
    {
        // Arrange — repo returns fewer projects than requested
        _projectRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(new List<Project>());

        CopyCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        CopyCostEstimateCommand command = ValidCommand();

        // Act
        TestValidationResult<CopyCostEstimateCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private CopyCostEstimateCommand ValidCommand() => new CopyCostEstimateCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = Guid.NewGuid(),
        TargetProjectIds = [_targetProjectId]
    };
}
