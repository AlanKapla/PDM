using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.ColdMails.SendColdMails;
using CQRS.PostCommit;
using Entities.Enums;
using Entities.Models.ColdMails;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.Admin;

public sealed class SendColdMailsCommandHandlerTests
{
    private readonly Mock<IRepository<ColdMailHistory>> _historyRepoMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IColdMailHtmlBuilder> _htmlBuilderMock = new();
    private readonly Mock<IPostCommitDispatcher> _postCommitMock = new();
    private readonly SendColdMailsCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private const string RenderedHtml =
        "<html><body>Hello Cold mail body Brickly https://app.brickly.pro</body></html>";

    public SendColdMailsCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        _htmlBuilderMock
            .Setup(b => b.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(RenderedHtml);

        _htmlBuilderMock
            .Setup(b => b.ToPlainText(It.IsAny<string>()))
            .Returns((string body) => body);

        _emailSenderMock
            .Setup(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _historyRepoMock
            .Setup(r => r.Insert(It.IsAny<ColdMailHistory>()))
            .Returns(Task.CompletedTask);

        _historyRepoMock
            .Setup(r => r.Update(It.IsAny<ColdMailHistory>()))
            .Returns(Task.CompletedTask);

        _historyRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Run post-commit actions immediately so enqueue/fail assertions still apply.
        _postCommitMock
            .Setup(d => d.Enqueue(It.IsAny<Func<CancellationToken, Task>>()))
            .Callback<Func<CancellationToken, Task>>(action =>
            {
                action(CancellationToken.None).GetAwaiter().GetResult();
            });

        _handler = new SendColdMailsCommandHandler(
            _historyRepoMock.Object,
            _emailSenderMock.Object,
            _currentUserMock.Object,
            _htmlBuilderMock.Object,
            _postCommitMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        Func<Task> act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenSenderSucceeds_SavesQueuedHistoryWithCorrelationId()
    {
        SendColdMailsResultWeb result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.QueuedCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(ColdMailStatus.Queued.ToString());

        _htmlBuilderMock.Verify(b => b.Build("Hello", "Cold mail body"), Times.Once);
        _htmlBuilderMock.Verify(b => b.ToPlainText("Cold mail body"), Times.Once);
        _historyRepoMock.Verify(
            r => r.Insert(It.Is<ColdMailHistory>(h =>
                h.RecipientEmail == "prospect@example.com"
                && h.Status == ColdMailStatus.Queued
                && h.ErrorMessage == null
                && h.SentByUserId == _currentUserId
                && h.BatchId == result.BatchId
                && h.Body == "Cold mail body"
                && h.HtmlBody == RenderedHtml)),
            Times.Once);
        _historyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _postCommitMock.Verify(
            d => d.Enqueue(It.IsAny<Func<CancellationToken, Task>>()),
            Times.Once);
        _emailSenderMock.Verify(
            s => s.SendEmailAsync(
                It.Is<EmailMessageDto>(m =>
                    m.To == "prospect@example.com"
                    && m.Subject == "Hello"
                    && m.TextBody == "Cold mail body"
                    && m.HtmlBody == RenderedHtml
                    && m.ColdMailHistoryId != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBodyIsHtml_StoresPlainTextInHistoryBody()
    {
        const string editorHtml = "<p><strong>Hej Alan</strong></p>";
        _htmlBuilderMock
            .Setup(b => b.ToPlainText(editorHtml))
            .Returns("Hej Alan");

        SendColdMailsCommand command = ValidCommand() with { Body = editorHtml };

        SendColdMailsResultWeb result = await _handler.Handle(command, CancellationToken.None);

        result.QueuedCount.Should().Be(1);
        _htmlBuilderMock.Verify(b => b.Build("Hello", editorHtml), Times.Once);
        _historyRepoMock.Verify(
            r => r.Insert(It.Is<ColdMailHistory>(h => h.Body == "Hej Alan")),
            Times.Once);
        _emailSenderMock.Verify(
            s => s.SendEmailAsync(
                It.Is<EmailMessageDto>(m => m.TextBody == "Hej Alan"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSenderThrows_SavesFailedHistory()
    {
        _emailSenderMock
            .Setup(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        SendColdMailsResultWeb result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        // Immediate post-commit mock runs during Enqueue, so Failed is visible in response.
        result.QueuedCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Items[0].Status.Should().Be(ColdMailStatus.Failed.ToString());
        result.Items[0].ErrorMessage.Should().Be("SMTP unavailable");

        _historyRepoMock.Verify(
            r => r.Update(It.Is<ColdMailHistory>(h =>
                h.Status == ColdMailStatus.Failed
                && h.ErrorMessage == "SMTP unavailable")),
            Times.Once);
        _historyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenDuplicateEmails_DedupesCaseInsensitive()
    {
        SendColdMailsCommand command = ValidCommand() with
        {
            Emails = new[] { "A@Example.com", "a@example.com", "other@example.com" }
        };

        SendColdMailsResultWeb result = await _handler.Handle(command, CancellationToken.None);

        result.QueuedCount.Should().Be(2);
        _postCommitMock.Verify(
            d => d.Enqueue(It.IsAny<Func<CancellationToken, Task>>()),
            Times.Exactly(2));
        _emailSenderMock.Verify(
            s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _historyRepoMock.Verify(r => r.Insert(It.IsAny<ColdMailHistory>()), Times.Exactly(2));
    }

    private static SendColdMailsCommand ValidCommand() => new()
    {
        Emails = new[] { "prospect@example.com" },
        Subject = "Hello",
        Body = "Cold mail body"
    };
}
