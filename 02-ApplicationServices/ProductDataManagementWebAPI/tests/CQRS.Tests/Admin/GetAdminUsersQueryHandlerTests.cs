using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.Users.GetAdminUsers;
using Entities.Enums;
using Entities.Models.Users;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Admin;

public sealed class GetAdminUsersQueryHandlerTests
{
    private readonly Mock<IReadRepository<User>> _userReadRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetAdminUsersQueryHandler _handler;

    public GetAdminUsersQueryHandlerTests()
    {
        _handler = new GetAdminUsersQueryHandler(
            _userReadRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_ReturnsUsersOrderedByCreatedAtDescending()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);
        User older = CreateUser("a@test.com", "Ada", "Nowak", DateTime.UtcNow.AddDays(-2));
        User newer = CreateUser("b@test.com", "Bartek", "Kowalski", DateTime.UtcNow.AddDays(-1));
        _userReadRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(new[] { older, newer });

        // Act
        IReadOnlyList<AdminUserWeb> result = await _handler.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("b@test.com");
        result[1].Email.Should().Be("a@test.com");
    }

    private static User CreateUser(string email, string firstName, string lastName, DateTime createdAt)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            SystemRole = SystemRole.User,
            CreatedAt = createdAt,
            WelcomeEmailSentAt = null
        };
    }
}
