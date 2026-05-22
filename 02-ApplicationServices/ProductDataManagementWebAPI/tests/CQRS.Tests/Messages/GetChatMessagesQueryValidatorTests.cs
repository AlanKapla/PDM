using Business.Interfaces.Model;
using CQRS.Messages.GetChatMessages;
using Entities.Models.Chats;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Messages;

public sealed class GetChatMessagesQueryValidatorTests
{
    private readonly Mock<IReadRepository<Chat>> _chatRepoMock = new();
    private readonly Mock<IRepository<ChatMember>> _chatMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _chatId = Guid.NewGuid();

    public GetChatMessagesQueryValidatorTests()
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
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { ChatId = Guid.Empty };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatId);
    }

    [Fact]
    public async Task Validate_WhenChatIdIsValid_HasNoSyncValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChatId);
    }

    // === PageNumber ===

    [Fact]
    public async Task Validate_WhenPageNumberIsZero_HasValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { PageNumber = 0 };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public async Task Validate_WhenPageNumberIsNegative_HasValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { PageNumber = -1 };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public async Task Validate_WhenPageNumberIsOne_HasNoValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { PageNumber = 1 };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
    }

    // === PageSize ===

    [Fact]
    public async Task Validate_WhenPageSizeIsZero_HasValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { PageSize = 0 };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task Validate_WhenPageSizeExceedsMaximum_HasValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { PageSize = 101 };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task Validate_WhenPageSizeIsWithinRange_HasNoValidationError()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery() with { PageSize = 50 };

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    // === Chat must exist ===

    [Fact]
    public async Task Validate_WhenChatDoesNotExist_HasValidationError()
    {
        // Arrange
        SetupChatExists(false);
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === User must be member ===

    [Fact]
    public async Task Validate_WhenUserIsNotChatMember_HasValidationError()
    {
        // Arrange
        SetupUserIsMember(false);
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
    }

    // === Happy path ===

    [Fact]
    public async Task Validate_WhenQueryIsValid_HasNoValidationErrors()
    {
        // Arrange
        GetChatMessagesQueryValidator validator = BuildValidator();
        GetChatMessagesQuery query = ValidQuery();

        // Act
        TestValidationResult<GetChatMessagesQuery> result =
            await validator.TestValidateAsync(query);

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

    private GetChatMessagesQueryValidator BuildValidator() =>
        new GetChatMessagesQueryValidator(
            _chatRepoMock.Object,
            _chatMemberRepoMock.Object,
            _currentUserMock.Object);

    private GetChatMessagesQuery ValidQuery() => new GetChatMessagesQuery(
        TenantId: Guid.NewGuid(),
        ProjectId: Guid.NewGuid(),
        ChatId: _chatId,
        PageNumber: 1,
        PageSize: 20
    );
}
