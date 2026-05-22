using Business.Interfaces.Model;
using CQRS.Messages.SendMessage;
using Entities.Models.Chats;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Messages;

public sealed class SendMessageCommandValidatorTests
{
    private readonly Mock<IReadRepository<Chat>> _chatRepoMock = new();
    private readonly Mock<IRepository<ChatMember>> _chatMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _chatId = Guid.NewGuid();

    public SendMessageCommandValidatorTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        // Default: chat exists, user is member
        SetupChatExists(true);
        SetupUserIsMember(true);
    }

    // === ChatId ===

    [Fact]
    public async Task Validate_WhenChatIdIsEmpty_HasValidationError()
    {
        // Arrange
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand() with { ChatId = Guid.Empty };

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatId);
    }

    [Fact]
    public async Task Validate_WhenChatIdIsValid_HasNoSyncValidationError()
    {
        // Arrange
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand();

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChatId);
    }

    // === Content ===

    [Fact]
    public async Task Validate_WhenContentIsEmpty_HasValidationError()
    {
        // Arrange
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand() with { Content = string.Empty };

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public async Task Validate_WhenContentExceedsMaxLength_HasValidationError()
    {
        // Arrange
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand() with { Content = new string('a', 4001) };

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public async Task Validate_WhenContentIsAtMaxLength_HasNoValidationError()
    {
        // Arrange
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand() with { Content = new string('a', 4000) };

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    // === Chat must exist ===

    [Fact]
    public async Task Validate_WhenChatDoesNotExist_HasValidationError()
    {
        // Arrange
        SetupChatExists(false);
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand();

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === User must be member ===

    [Fact]
    public async Task Validate_WhenUserIsNotChatMember_HasValidationError()
    {
        // Arrange
        SetupUserIsMember(false);
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand();

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        // Arrange
        SendMessageCommandValidator validator = BuildValidator();
        SendMessageCommand command = ValidCommand();

        // Act
        TestValidationResult<SendMessageCommand> result =
            await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // === Helpers ===

    private void SetupChatExists(bool exists)
    {
        Chat? chat = exists ? Chat.CreateDirect(Guid.NewGuid(), Guid.NewGuid(), "test-chat") : null;

        _chatRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Chat, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);
    }

    private void SetupUserIsMember(bool isMember)
    {
        ChatMember? member = isMember
            ? new ChatMember(_chatId, _currentUserId, false)
            : null;

        _chatMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ChatMember, bool>>>(),
                It.IsAny<Func<IQueryable<ChatMember>, IIncludableQueryable<ChatMember, object>>[]>()))
            .ReturnsAsync(member);
    }

    private SendMessageCommandValidator BuildValidator() =>
        new SendMessageCommandValidator(
            _chatRepoMock.Object,
            _chatMemberRepoMock.Object,
            _currentUserMock.Object);

    private SendMessageCommand ValidCommand() => new SendMessageCommand(
        TenantId: Guid.NewGuid(),
        ProjectId: Guid.NewGuid(),
        ChatId: _chatId,
        Content: "Hello world"
    );
}
