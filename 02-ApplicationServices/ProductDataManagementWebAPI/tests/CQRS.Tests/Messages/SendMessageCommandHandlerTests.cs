using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Messages.SendMessage;
using Entities.Models.Chats;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.Messages;

public sealed class SendMessageCommandHandlerTests
{
    private readonly Mock<IRepository<MessageHistory>> _messageRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IQueueStorageService> _queueStorageMock = new();
    private readonly SendMessageCommandHandler _handler;

    private static readonly Guid _userId = Guid.NewGuid();

    public SendMessageCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);
        _handler = new SendMessageCommandHandler(
            _messageRepoMock.Object,
            _currentUserMock.Object,
            _queueStorageMock.Object);
    }

    private static SendMessageCommand ValidCommand() =>
        new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Hello world");

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsMessageAndEnqueues()
    {
        // Arrange
        SendMessageCommand command = ValidCommand();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _messageRepoMock.Verify(r => r.Insert(It.IsAny<MessageHistory>()), Times.Once);
        _queueStorageMock.Verify(q => q.EnqueueAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_UsesCurrentUserId()
    {
        // Arrange
        SendMessageCommand command = ValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _currentUserMock.Verify(u => u.Id, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_ReturnsNonEmptyGuid()
    {
        // Arrange
        SendMessageCommand command = ValidCommand();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }
}
