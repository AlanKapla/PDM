using Business.Interfaces.Model;
using CQRS.Tenants.RemoveTenantInvitation;
using Entities.Models.Tenants;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class RemoveTenantInvitationCommandValidatorTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _invitationId = Guid.NewGuid();

    public RemoveTenantInvitationCommandValidatorTests()
    {
        // Default: repos return valid entities
        SetupTenantExists(true);
        SetupInvitationExists(true);
    }

    // === TenantId ===

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand() with { TenantId = Guid.Empty };

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_HasNoValidationError()
    {
        // Arrange
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // === InvitationId ===

    [Fact]
    public async Task Validate_WhenInvitationIdIsEmpty_HasValidationError()
    {
        // Arrange
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand() with { InvitationId = Guid.Empty };

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvitationId);
    }

    [Fact]
    public async Task Validate_WhenInvitationIdIsValid_HasNoValidationError()
    {
        // Arrange
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.InvitationId);
    }

    // === TenantMustExist ===

    [Fact]
    public async Task Validate_WhenTenantDoesNotExist_HasValidationError()
    {
        // Arrange
        SetupTenantExists(false);
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === InvitationMustExist ===

    [Fact]
    public async Task Validate_WhenInvitationDoesNotExist_HasValidationError()
    {
        // Arrange
        SetupInvitationExists(false);
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        RemoveTenantInvitationCommandValidator validator = BuildValidator();
        RemoveTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<RemoveTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helpers ===

    private void SetupTenantExists(bool exists)
    {
        Tenant? tenant = exists
            ? new Tenant { Id = _tenantId, IsActive = true, Name = "Test Tenant" }
            : null;

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
    }

    private void SetupInvitationExists(bool exists)
    {
        TenantInvitation? invitation = exists
            ? new TenantInvitation { Id = _invitationId, TenantId = _tenantId }
            : null;

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);
    }

    private RemoveTenantInvitationCommandValidator BuildValidator() =>
        new RemoveTenantInvitationCommandValidator(
            _tenantRepoMock.Object,
            _invitationRepoMock.Object,
            _currentUserMock.Object);

    private RemoveTenantInvitationCommand ValidCommand() => new RemoveTenantInvitationCommand
    {
        TenantId = _tenantId,
        InvitationId = _invitationId
    };
}
