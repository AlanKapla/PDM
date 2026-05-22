using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Messages;
using CQRS.Messages.GetChatMessages;
using Entities.Models.Chats;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Messages;

public sealed class GetChatMessagesQueryHandlerTests
{
    private readonly Mock<IReadRepository<MessageHistory>> _messageRepoMock = new();
    private readonly Mock<IProjectMemberService> _projectMemberServiceMock = new();
    private readonly GetChatMessagesQueryHandler _handler;

    public GetChatMessagesQueryHandlerTests()
    {
        _handler = new GetChatMessagesQueryHandler(
            _messageRepoMock.Object,
            _projectMemberServiceMock.Object);
    }

    private static MessageHistory CreateMessage(Guid chatId, Guid userId) =>
        MessageHistory.Create(chatId, userId, "Hello world", null);

    [Fact]
    public async Task Handle_WhenMessagesExist_ReturnsMappedMessages()
    {
        // Arrange
        Guid chatId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        List<MessageHistory> messages = new()
        {
            CreateMessage(chatId, userId),
            CreateMessage(chatId, userId)
        };

        Dictionary<Guid, (string FirstName, string LastName)> userNames = new()
        {
            { userId, ("John", "Doe") }
        };

        _messageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<MessageHistory, bool>>>(),
                It.IsAny<Func<IQueryable<MessageHistory>, IIncludableQueryable<MessageHistory, object>>[]>()))
            .ReturnsAsync(messages);

        _projectMemberServiceMock
            .Setup(s => s.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userNames);

        GetChatMessagesQuery query = new(
            TenantId: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            ChatId: chatId,
            PageNumber: 1,
            PageSize: 50);

        // Act
        List<MessageWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].UserFirstName.Should().Be("John");
        result[0].UserLastName.Should().Be("Doe");
    }

    [Fact]
    public async Task Handle_WhenNoMessages_ReturnsEmpty()
    {
        // Arrange
        _messageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<MessageHistory, bool>>>(),
                It.IsAny<Func<IQueryable<MessageHistory>, IIncludableQueryable<MessageHistory, object>>[]>()))
            .ReturnsAsync(new List<MessageHistory>());

        _projectMemberServiceMock
            .Setup(s => s.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (string, string)>());

        GetChatMessagesQuery query = new(
            TenantId: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            ChatId: Guid.NewGuid(),
            PageNumber: 1,
            PageSize: 50);

        // Act
        List<MessageWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsOne_ReturnsSingleMessage()
    {
        // Arrange
        Guid chatId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        List<MessageHistory> messages = new()
        {
            CreateMessage(chatId, userId),
            CreateMessage(chatId, userId),
            CreateMessage(chatId, userId)
        };

        _messageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<MessageHistory, bool>>>(),
                It.IsAny<Func<IQueryable<MessageHistory>, IIncludableQueryable<MessageHistory, object>>[]>()))
            .ReturnsAsync(messages);

        _projectMemberServiceMock
            .Setup(s => s.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (string, string)>());

        GetChatMessagesQuery query = new(
            TenantId: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            ChatId: chatId,
            PageNumber: 1,
            PageSize: 1);

        // Act
        List<MessageWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
    }
}
