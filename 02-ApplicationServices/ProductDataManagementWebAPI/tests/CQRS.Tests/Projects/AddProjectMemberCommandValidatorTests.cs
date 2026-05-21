using CQRS.Projects.AddProjectMember;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using FluentValidation.TestHelper;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class AddProjectMemberCommandValidatorTests
{
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly AddProjectMemberCommandValidator _validator;

    public AddProjectMemberCommandValidatorTests()
    {
        // Default: user IS active tenant member, user is NOT yet a project member
        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TenantMember, bool>>>()))
            .ReturnsAsync(new TenantMember());

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync((ProjectMember?)null);

        _validator = new AddProjectMemberCommandValidator(
            _projectMemberRepoMock.Object,
            _tenantMemberRepoMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === UserId ===

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand() with { UserId = Guid.Empty };

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsValid_HasNoValidationError()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    // === Async rule: UserMustBeTenantMember ===

    [Fact]
    public async Task Validate_WhenUserIsNotTenantMember_HasValidationError()
    {
        // Arrange
        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TenantMember, bool>>>()))
            .ReturnsAsync((TenantMember?)null);

        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public async Task Validate_WhenUserIsActiveTenantMember_HasNoTenantMemberValidationError()
    {
        // Arrange — default setup already returns a tenant member
        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    // === Async rule: UserMustNotBeProjectMember ===

    [Fact]
    public async Task Validate_WhenUserIsAlreadyProjectMember_HasValidationError()
    {
        // Arrange
        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(new ProjectMember());

        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public async Task Validate_WhenUserIsNotYetProjectMember_HasNoProjectMemberValidationError()
    {
        // Arrange — default setup already returns null (not a project member)
        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<AddProjectMemberCommand> result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static AddProjectMemberCommand ValidCommand() => new AddProjectMemberCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
    };
}
