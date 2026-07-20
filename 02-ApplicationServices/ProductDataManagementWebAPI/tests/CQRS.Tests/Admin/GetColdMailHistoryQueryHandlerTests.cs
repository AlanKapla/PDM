using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.ColdMails.GetColdMailHistory;
using Entities.Enums;
using Entities.Models.ColdMails;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Admin;

public sealed class GetColdMailHistoryQueryHandlerTests
{
    private readonly Mock<IReadRepository<ColdMailHistory>> _historyRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetColdMailHistoryQueryHandler _handler;

    public GetColdMailHistoryQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        _handler = new GetColdMailHistoryQueryHandler(
            _historyRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        Func<Task> act = async () => await _handler.Handle(
            new GetColdMailHistoryQuery(null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenEmailFilterProvided_PassesFilterToRepository()
    {
        ColdMailHistory matching = CreateHistory("prospect@acme.com");
        SetupPagedResult(new List<ColdMailHistory> { matching });

        IReadOnlyList<ColdMailHistoryWeb> result = await _handler.Handle(
            new GetColdMailHistoryQuery("acme"),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].RecipientEmail.Should().Be("prospect@acme.com");
        result[0].Status.Should().Be(ColdMailStatus.Queued.ToString());

        _historyRepoMock.Verify(
            r => r.GetPagedBySearchAsync(
                It.IsAny<Expression<Func<ColdMailHistory, bool>>>(),
                It.IsAny<Expression<Func<ColdMailHistory, DateTime>>>(),
                true,
                0,
                500,
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<ColdMailHistory>, IIncludableQueryable<ColdMailHistory, object>>[]>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoFilter_ReturnsMappedHistory()
    {
        ColdMailHistory first = CreateHistory("a@test.com");
        ColdMailHistory second = CreateHistory("b@test.com");
        SetupPagedResult(new List<ColdMailHistory> { first, second });

        IReadOnlyList<ColdMailHistoryWeb> result = await _handler.Handle(
            new GetColdMailHistoryQuery(null),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].RecipientEmail.Should().Be("a@test.com");
        result[1].RecipientEmail.Should().Be("b@test.com");
    }

    private void SetupPagedResult(List<ColdMailHistory> items)
    {
        _historyRepoMock
            .Setup(r => r.GetPagedBySearchAsync(
                It.IsAny<Expression<Func<ColdMailHistory, bool>>>(),
                It.IsAny<Expression<Func<ColdMailHistory, DateTime>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<ColdMailHistory>, IIncludableQueryable<ColdMailHistory, object>>[]>()))
            .ReturnsAsync(items);
    }

    private static ColdMailHistory CreateHistory(string email)
    {
        return new ColdMailHistory
        {
            Id = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            RecipientEmail = email,
            Subject = "Subject",
            Body = "Body",
            HtmlBody = "<html><body>Body</body></html>",
            Status = ColdMailStatus.Queued,
            SentByUserId = Guid.NewGuid(),
            SentAt = DateTime.UtcNow
        };
    }
}
