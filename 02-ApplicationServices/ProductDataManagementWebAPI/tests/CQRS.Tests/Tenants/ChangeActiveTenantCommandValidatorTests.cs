using Business.Interfaces.Model;
using CQRS.Tenants.ChangeActiveTenant;
using Entities.Models.Tenants;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class ChangeActiveTenantCommandValidatorTests
{
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    // === IsAuthenticated ===

    [Fact]
    public async Task Validate_WhenUserIsNotAuthenticated_HasValidationError()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);
        SetupMemberExists(false);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);
        ChangeActiveTenantCommandValidator validator = BuildValidator();
        ChangeActiveTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<ChangeActiveTenantCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        SetupAuthenticated();
        ChangeActiveTenantCommandValidator validator = BuildValidator();
        ChangeActiveTenantCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<ChangeActiveTenantCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenUserIsMember_HasNoValidationError()
    {
        // Arrange
        SetupAuthenticated();
        SetupMemberExists(exists: true);
        ChangeActiveTenantCommandValidator validator = BuildValidator();
        ChangeActiveTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<ChangeActiveTenantCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenUserIsNotMemberAndNotSuperAdmin_HasValidationError()
    {
        // Arrange
        SetupAuthenticated();
        SetupMemberExists(exists: false);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);
        ChangeActiveTenantCommandValidator validator = BuildValidator();
        ChangeActiveTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<ChangeActiveTenantCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenUserIsNotMemberButIsSuperAdmin_HasNoValidationError()
    {
        // Arrange
        SetupAuthenticated();
        SetupMemberExists(exists: false);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);
        ChangeActiveTenantCommandValidator validator = BuildValidator();
        ChangeActiveTenantCommand command = ValidCommand();

        // Act
        TestValidationResult<ChangeActiveTenantCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helpers ===

    private void SetupAuthenticated()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);
    }

    private void SetupMemberExists(bool exists)
    {
        TenantMember? member = exists
            ? new TenantMember { TenantId = Guid.NewGuid(), UserId = _currentUserId, IsActive = true }
            : null;

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(member);
    }

    private ChangeActiveTenantCommandValidator BuildValidator() =>
        new ChangeActiveTenantCommandValidator(_tenantMemberRepoMock.Object, _currentUserMock.Object);

    private static ChangeActiveTenantCommand ValidCommand() => new ChangeActiveTenantCommand
    {
        TenantId = Guid.NewGuid()
    };
}
