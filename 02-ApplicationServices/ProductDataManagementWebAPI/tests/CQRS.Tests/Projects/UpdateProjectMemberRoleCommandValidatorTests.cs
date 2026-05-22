using Business.Interfaces.Model;
using CQRS.Projects.UpdateProjectMemberRole;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Projects;

public sealed class UpdateProjectMemberRoleCommandValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateProjectMemberRoleCommandValidator _validator;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public UpdateProjectMemberRoleCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);
        _validator = new UpdateProjectMemberRoleCommandValidator(_currentUserMock.Object);
    }

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === UserId ===

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand() with { UserId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsCurrentUser_HasValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand() with { UserId = _currentUserId };

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsNotCurrentUser_HasNoValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    // === RoleId ===

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_HasValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand() with { RoleId = Guid.Empty };

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public void Validate_WhenRoleIdIsValid_HasNoValidationError()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();

        // Act
        TestValidationResult<UpdateProjectMemberRoleCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private UpdateProjectMemberRoleCommand ValidCommand() => new UpdateProjectMemberRoleCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        RoleId = Guid.NewGuid(),
    };
}
