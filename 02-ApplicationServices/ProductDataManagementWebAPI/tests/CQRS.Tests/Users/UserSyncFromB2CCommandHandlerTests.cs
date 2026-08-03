using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Users.UserSyncFromB2C;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Users;

public sealed class UserSyncFromB2CCommandHandlerTests
{
    private readonly Mock<IReadRepository<User>> _userReadRepoMock = new();
    private readonly Mock<IRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IMicrosoftGraphService> _graphServiceMock = new();
    private readonly Mock<IWelcomeEmailService> _welcomeEmailServiceMock = new();
    private readonly Mock<ILogger<UserSyncFromB2CCommandHandler>> _loggerMock = new();
    private readonly UserSyncFromB2CCommandHandler _handler;

    private const string ValidB2CObjectId = "oid-abc-123";
    private const string ValidEmail = "user@example.com";

    public UserSyncFromB2CCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.AzureAdB2CObjectId).Returns(ValidB2CObjectId);
        _currentUserMock.Setup(u => u.Email).Returns(ValidEmail);

        _userReadRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        _graphServiceMock
            .Setup(g => g.GetUserDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserGraphData("John", "Doe"));

        _welcomeEmailServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new UserSyncFromB2CCommandHandler(
            _userReadRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object,
            _graphServiceMock.Object,
            _welcomeEmailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        UserSyncFromB2CCommand command = new();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedApiException>();
    }

    [Fact]
    public async Task Handle_WhenAzureB2CObjectIdIsEmpty_ThrowsValidationApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.AzureAdB2CObjectId).Returns(string.Empty);
        UserSyncFromB2CCommand command = new();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenEmailIsEmpty_ThrowsValidationApiException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.Email).Returns(string.Empty);
        UserSyncFromB2CCommand command = new();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenUserExistsByB2CId_ReturnsExistingUserId()
    {
        // Arrange
        User existingUser = new() { Id = Guid.NewGuid(), AzureAdB2CObjectId = ValidB2CObjectId };

        _userReadRepoMock
            .SetupSequence(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(existingUser);

        UserSyncFromB2CCommand command = new();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existingUser.Id);
        _userRepoMock.Verify(r => r.Insert(It.IsAny<User>()), Times.Never);
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExistsByEmail_LinksB2CAndReturnsUserId()
    {
        // Arrange
        User existingUser = new() { Id = Guid.NewGuid(), Email = ValidEmail, AzureAdB2CObjectId = string.Empty };

        _userReadRepoMock
            .SetupSequence(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(existingUser);

        UserSyncFromB2CCommand command = new();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existingUser.Id);
        existingUser.AzureAdB2CObjectId.Should().Be(ValidB2CObjectId);
        _userRepoMock.Verify(r => r.Update(existingUser), Times.Once);
        _userRepoMock.Verify(r => r.Insert(It.IsAny<User>()), Times.Never);
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNewUserCreated_SendsWelcomeEmailAndSetsSentAt()
    {
        // Arrange
        UserSyncFromB2CCommand command = new();
        User? capturedUser = null;

        _userRepoMock
            .Setup(r => r.Insert(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        capturedUser.Should().NotBeNull();
        capturedUser!.WelcomeEmailSentAt.Should().NotBeNull();
        _userRepoMock.Verify(r => r.Insert(It.IsAny<User>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_DoesNotSendWelcomeEmail()
    {
        // Arrange
        User existingUser = new()
        {
            Id = Guid.NewGuid(),
            AzureAdB2CObjectId = ValidB2CObjectId
        };

        _userReadRepoMock
            .SetupSequence(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(existingUser);

        UserSyncFromB2CCommand command = new();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existingUser.Id);
        _welcomeEmailServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsNew_CreatesUserAndReturnsNewId()
    {
        // Arrange
        UserSyncFromB2CCommand command = new();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _userRepoMock.Verify(r => r.Insert(It.IsAny<User>()), Times.Once);
    }
}
