using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using CQRS.Users.UserUpdate;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Users;

public sealed class UserUpdateCommandHandlerTests
{
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UserUpdateCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public UserUpdateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);
        _handler = new UserUpdateCommandHandler(_userRepoMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesFieldsAndReturnsUserUpdateWeb()
    {
        // Arrange
        User user = new() { Id = _userId, FirstName = "Old", LastName = "Name" };

        _userRepoMock
            .Setup(r => r.GetById(
                It.IsAny<Guid>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(user);

        UserUpdateCommand command = new(
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "555-0000",
            CompanyName: "Acme",
            TaxId: "TAX999",
            Street: "1st Avenue",
            City: "New York",
            PostalCode: "10001",
            Country: "USA");

        // Act
        UserUpdateWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.PhoneNumber.Should().Be("555-0000");
        result.CompanyName.Should().Be("Acme");
        result.City.Should().Be("New York");
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetById(
                It.IsAny<Guid>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        UserUpdateCommand command = new(
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: null,
            CompanyName: null,
            TaxId: null,
            Street: null,
            City: null,
            PostalCode: null,
            Country: null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
