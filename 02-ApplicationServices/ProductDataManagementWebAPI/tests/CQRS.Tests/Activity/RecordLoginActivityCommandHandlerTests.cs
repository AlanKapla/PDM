using Business.Interfaces.Model;
using CQRS.Activity.RecordLoginActivity;
using Entities.Enums;
using Entities.Models.Activity;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Activity;

public sealed class RecordLoginActivityCommandHandlerTests
{
    private readonly Mock<IRepository<UserActivityLog>> _activityLogRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly RecordLoginActivityCommandHandler _handler;

    public RecordLoginActivityCommandHandlerTests()
    {
        _handler = new RecordLoginActivityCommandHandler(
            _activityLogRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_InsertsLoginWithUserId()
    {
        // Arrange
        string oid = Guid.NewGuid().ToString();
        Guid userId = Guid.NewGuid();
        User user = new()
        {
            Id = userId,
            AzureAdB2CObjectId = oid,
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        _currentUserMock.Setup(u => u.AzureAdB2CObjectId).Returns(oid);
        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(user);

        RecordLoginActivityCommand command = new()
        {
            IpAddress = "1.2.3.4",
            Route = "/auth/callback"
        };

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _activityLogRepoMock.Verify(
            r => r.Insert(It.Is<UserActivityLog>(l =>
                l.EventType == UserActivityEventType.Login
                && l.IpAddress == "1.2.3.4"
                && l.Route == "/auth/callback"
                && l.UserId == userId
                && l.AzureAdB2CObjectId == oid)),
            Times.Once);
        _activityLogRepoMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotSynced_InsertsLoginWithNullUserId()
    {
        // Arrange
        string oid = Guid.NewGuid().ToString();
        _currentUserMock.Setup(u => u.AzureAdB2CObjectId).Returns(oid);
        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        RecordLoginActivityCommand command = new()
        {
            IpAddress = "10.0.0.1",
            Route = null
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _activityLogRepoMock.Verify(
            r => r.Insert(It.Is<UserActivityLog>(l =>
                l.EventType == UserActivityEventType.Login
                && l.UserId == null
                && l.AzureAdB2CObjectId == oid)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOidMissing_FallsBackToShortOidClaim()
    {
        // Arrange
        string oid = "short-oid-value";
        _currentUserMock.Setup(u => u.AzureAdB2CObjectId).Returns(string.Empty);
        _currentUserMock.Setup(u => u.GetClaimValue("oid")).Returns(oid);
        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        RecordLoginActivityCommand command = new()
        {
            IpAddress = "127.0.0.1"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _activityLogRepoMock.Verify(
            r => r.Insert(It.Is<UserActivityLog>(l => l.AzureAdB2CObjectId == oid)),
            Times.Once);
    }
}
