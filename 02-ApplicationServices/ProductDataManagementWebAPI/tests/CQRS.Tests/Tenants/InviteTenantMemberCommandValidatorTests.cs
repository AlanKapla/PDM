using Business.Interfaces.Model;
using CQRS.Tenants.InviteTenantMember;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class InviteTenantMemberCommandValidatorTests
{
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly InviteTenantMemberCommandValidator _validator;

    public InviteTenantMemberCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Email).Returns("inviter@example.com");
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        // Default: user not found (not a member)
        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _validator = new InviteTenantMemberCommandValidator(
            _tenantMemberRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === Email ===

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_HasValidationError()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand() with { Email = string.Empty };

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenEmailExceedsMaxLength_HasValidationError()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand() with
        {
            Email = new string('a', 315) + "@b.com"
        };

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalidFormat_HasValidationError()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand() with { Email = "not-an-email" };

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenEmailIsSameAsCurrentUser_HasValidationError()
    {
        // Arrange — same email as current user
        InviteTenantMemberCommand command = ValidCommand() with { Email = "inviter@example.com" };

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // === Already a member ===

    [Fact]
    public async Task Validate_WhenUserIsAlreadyMember_HasValidationError()
    {
        // Arrange
        User existingUser = new User { Id = Guid.NewGuid(), Email = "invited@example.com", IsActive = true };
        TenantMember existingMembership = new TenantMember { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(existingMembership);

        InviteTenantMemberCommandValidator validator = new(
            _tenantMemberRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);

        InviteTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Invitation already exists — allowed (handler extends and resends) ===

    [Fact]
    public async Task Validate_WhenActiveInvitationAlreadyExists_HasNoValidationError()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        InviteTenantMemberCommand command = ValidCommand();

        // Act
        TestValidationResult<InviteTenantMemberCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helper ===

    private static InviteTenantMemberCommand ValidCommand() => new InviteTenantMemberCommand
    {
        TenantId = Guid.NewGuid(),
        Email = "invited@example.com"
    };
}
