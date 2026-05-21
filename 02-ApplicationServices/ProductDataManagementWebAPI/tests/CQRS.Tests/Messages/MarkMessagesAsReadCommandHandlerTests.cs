using Business.Interfaces.Model;
using CQRS.Messages.MarkMessagesAsRead;
using Entities.Models.Chats;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Messages;

public sealed class MarkMessagesAsReadCommandHandlerTests
{
    private readonly Mock<IRepository<ChatMember>> _chatMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly MarkMessagesAsReadCommandHandler _handler;

    private static readonly Guid _userId = Guid.NewGuid();

    public MarkMessagesAsReadCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);
        _handler = new MarkMessagesAsReadCommandHandler(
            _chatMemberRepoMock.Object,
            _currentUserMock.Object);
    }

    private static MarkMessagesAsReadCommand ValidCommand() =>
        new MarkMessagesAsReadCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Handle_WhenChatMemberExists_MarksReadAndReturns1()
    {
        // Arrange
        MarkMessagesAsReadCommand command = ValidCommand();
        ChatMember member = new ChatMember(command.ChatId, _userId, false);

        _chatMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ChatMember, bool>>>(),
                It.IsAny<Func<IQueryable<ChatMember>, IIncludableQueryable<ChatMember, object>>[]>()))
            .ReturnsAsync(member);

        // Act
        int result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _chatMemberRepoMock.Verify(r => r.Update(member), Times.Once);
        _chatMemberRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenChatMemberNotFound_Returns0()
    {
        // Arrange
        MarkMessagesAsReadCommand command = ValidCommand();

        _chatMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ChatMember, bool>>>(),
                It.IsAny<Func<IQueryable<ChatMember>, IIncludableQueryable<ChatMember, object>>[]>()))
            .ReturnsAsync((ChatMember?)null);

        // Act
        int result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _chatMemberRepoMock.Verify(r => r.Update(It.IsAny<ChatMember>()), Times.Never);
        _chatMemberRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExecuted_ReturnsIntResult()
    {
        // Arrange
        MarkMessagesAsReadCommand command = ValidCommand();

        _chatMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ChatMember, bool>>>(),
                It.IsAny<Func<IQueryable<ChatMember>, IIncludableQueryable<ChatMember, object>>[]>()))
            .ReturnsAsync((ChatMember?)null);

        // Act
        int result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0);
    }
}
