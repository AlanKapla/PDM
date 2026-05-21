using Business.Interfaces.Model;
using CQRS.Tenants.AcceptTenantInvitation;
using Entities.Enums;
using Entities.Models.Tenants;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class AcceptTenantInvitationCommandValidatorTests
{
    private readonly Mock<IRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AcceptTenantInvitationCommandValidator _validator;

    private readonly Guid _invitedByUserId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private const string CurrentUserEmail = "user@example.com";

    public AcceptTenantInvitationCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);
        _currentUserMock.Setup(u => u.Email).Returns(CurrentUserEmail);

        SetupInvitationRepoReturns(BuildValidInvitation());

        _validator = new AcceptTenantInvitationCommandValidator(
            _invitationRepoMock.Object,
            _currentUserMock.Object);
    }

    // === Token (sync) ===

    [Fact]
    public async Task Validate_WhenTokenIsEmpty_HasValidationError()
    {
        // Arrange
        AcceptTenantInvitationCommand command = ValidCommand() with { Token = string.Empty };

        // Act
        TestValidationResult<AcceptTenantInvitationCommand> result =
            await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public async Task Validate_WhenTokenIsNotEmpty_HasNoSyncValidationError()
    {
        // Arrange
        SetupInvitationRepoReturns(BuildValidInvitation());
        AcceptTenantInvitationCommandValidator validator = new(
            _invitationRepoMock.Object,
            _currentUserMock.Object);
        AcceptTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<AcceptTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Token);
    }

    // === Token (async — invitation lookup) ===

    [Fact]
    public async Task Validate_WhenInvitationNotFound_HasValidationError()
    {
        // Arrange
        SetupInvitationRepoReturns(null);
        AcceptTenantInvitationCommandValidator validator = new(
            _invitationRepoMock.Object,
            _currentUserMock.Object);

        AcceptTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<AcceptTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public async Task Validate_WhenInvitationIsValid_HasNoValidationErrors()
    {
        // Arrange
        SetupInvitationRepoReturns(BuildValidInvitation());
        AcceptTenantInvitationCommandValidator validator = new(
            _invitationRepoMock.Object,
            _currentUserMock.Object);

        AcceptTenantInvitationCommand command = ValidCommand();

        // Act
        TestValidationResult<AcceptTenantInvitationCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helpers ===

    private void SetupInvitationRepoReturns(TenantInvitation? invitation)
    {
        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);
    }

    private TenantInvitation BuildValidInvitation() => new TenantInvitation
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Token = "valid-token",
        IsActive = true,
        Status = InvitationStatus.Pending,
        Email = CurrentUserEmail,
        InvitedByUserId = _invitedByUserId,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow
    };

    private static AcceptTenantInvitationCommand ValidCommand() => new AcceptTenantInvitationCommand
    {
        Token = "valid-token"
    };
}
