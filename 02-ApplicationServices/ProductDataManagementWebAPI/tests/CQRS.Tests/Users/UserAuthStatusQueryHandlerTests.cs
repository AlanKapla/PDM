using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.Users.UserAuthStatus;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Users;

public sealed class UserAuthStatusQueryHandlerTests
{
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UserAuthStatusQueryHandler _handler;

    public UserAuthStatusQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _handler = new UserAuthStatusQueryHandler(_userRepoMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsAuthStatusWithAllFalse()
    {
        // Arrange
        User user = new() { Id = Guid.NewGuid(), Email = "test@test.com" };

        _userRepoMock
            .Setup(r => r.GetById(
                It.IsAny<Guid>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(user);

        UserAuthStatusQuery query = new();

        // Act
        UserAuthStatusWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasLocalAuth.Should().BeFalse();
        result.HasGoogleAuth.Should().BeFalse();
        result.IsHybridAuth.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedApiException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetById(
                It.IsAny<Guid>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        UserAuthStatusQuery query = new();

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedApiException>();
    }
}
