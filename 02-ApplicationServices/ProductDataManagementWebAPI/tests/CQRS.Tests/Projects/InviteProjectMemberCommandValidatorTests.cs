using Business.Interfaces.Model;
using CQRS.Projects.InviteProjectMember;
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Users;
using FluentValidation.TestHelper;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class InviteProjectMemberCommandValidatorTests
{
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly InviteProjectMemberCommandValidator _validator;

    public InviteProjectMemberCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Email).Returns("inviter@example.com");
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync((ProjectMember?)null);

        _validator = new InviteProjectMemberCommandValidator(
            _projectMemberRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        InviteProjectMemberCommand command = ValidCommand() with { TenantId = Guid.Empty };

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenProjectIdIsEmpty_HasValidationError()
    {
        InviteProjectMemberCommand command = ValidCommand() with { ProjectId = Guid.Empty };

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_HasValidationError()
    {
        InviteProjectMemberCommand command = ValidCommand() with { Email = string.Empty };

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenEmailIsSameAsCurrentUser_HasValidationError()
    {
        InviteProjectMemberCommand command = ValidCommand() with { Email = "inviter@example.com" };

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenNoModulesAndNotAdmin_HasValidationError()
    {
        InviteProjectMemberCommand command = ValidCommand() with
        {
            IsAdmin = false,
            Modules = Array.Empty<ProjectModule>()
        };

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public async Task Validate_WhenUserIsAlreadyProjectMember_HasValidationError()
    {
        User existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "invited@example.com",
            IsActive = true
        };

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(new ProjectMember { IsActive = true });

        InviteProjectMemberCommand command = ValidCommand();

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public async Task Validate_WhenPendingInvitationWouldExist_HasNoValidationError()
    {
        InviteProjectMemberCommand command = ValidCommand();

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        InviteProjectMemberCommand command = ValidCommand();

        TestValidationResult<InviteProjectMemberCommand> result =
            await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static InviteProjectMemberCommand ValidCommand() => new InviteProjectMemberCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Email = "invited@example.com",
        IsAdmin = false,
        Modules = new List<ProjectModule> { ProjectModule.Files }
    };
}
