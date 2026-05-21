using Business.Interfaces.Model;
using CQRS.Projects.RemoveProjectMember;
using FluentValidation.TestHelper;
using Moq;

namespace CQRS.Tests.Projects;

public sealed class RemoveProjectMemberCommandValidatorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly RemoveProjectMemberCommandValidator _validator;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public RemoveProjectMemberCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);
        _validator = new RemoveProjectMemberCommandValidator(_currentUserMock.Object);
    }

    // === TenantId ===

    [Fact]
    public void Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === ProjectId ===

    [Fact]
    public void Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_WhenProjectIdIsValid_HasNoValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProjectId);
    }

    // === UserId ===

    [Fact]
    public void Validate_WhenUserIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand() with { UserId = Guid.Empty };

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsCurrentUser_HasValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand() with { UserId = _currentUserId };

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenUserIdIsNotCurrentUser_HasNoValidationError()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    // === Happy path ===

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveProjectMemberCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private RemoveProjectMemberCommand ValidCommand() => new RemoveProjectMemberCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
    };
}
