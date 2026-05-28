using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using CQRS.Users.UserDetails;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Users;

public sealed class UserDetailsQueryHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly UserDetailsQueryHandler _handler;

    public UserDetailsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.FirstName).Returns("John");
        _currentUserMock.Setup(u => u.LastName).Returns("Doe");
        _currentUserMock.Setup(u => u.Email).Returns("john@example.com");
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(u => u.ActiveTenantId).Returns((Guid?)null);

        _userRepoMock
            .Setup(r => r.GetById(
                It.IsAny<Guid>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        _handler = new UserDetailsQueryHandler(_currentUserMock.Object, _userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsDetailsWithEmptyPermissions()
    {
        // Arrange
        UserDetailsQuery query = new();

        // Act
        UserDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john@example.com");
        result.IsActiveTenantAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserExistsInDb_ReturnsUserProfileFields()
    {
        // Arrange
        User user = new()
        {
            PhoneNumber = "123456789",
            CompanyName = "Acme Corp",
            TaxId = "TAX123",
            Street = "Main St",
            City = "Warsaw",
            PostalCode = "00-001",
            Country = "Poland"
        };

        _userRepoMock
            .Setup(r => r.GetById(
                It.IsAny<Guid>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(user);

        UserDetailsQuery query = new();

        // Act
        UserDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PhoneNumber.Should().Be("123456789");
        result.CompanyName.Should().Be("Acme Corp");
        result.TaxId.Should().Be("TAX123");
        result.City.Should().Be("Warsaw");
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticatedAndHasActiveTenant_ReturnsTenantPermissions()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.ActiveTenantId).Returns(tenantId);
        _currentUserMock
            .Setup(u => u.GetActiveTenantSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCtxSnapshot(
                TenantId: tenantId,
                IsAdmin: true,
                IsActive: true));

        UserDetailsQuery query = new();

        // Act
        UserDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsActiveTenantAdmin.Should().BeTrue();
    }
}
