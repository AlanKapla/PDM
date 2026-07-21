using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.ActivityLogs.GetUserActivityLogs;
using Entities.Enums;
using Entities.Models.Activity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Admin;

public sealed class GetUserActivityLogsQueryHandlerTests
{
    private readonly Mock<IReadRepository<UserActivityLog>> _activityLogRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetUserActivityLogsQueryHandler _handler;

    public GetUserActivityLogsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        _handler = new GetUserActivityLogsQueryHandler(
            _activityLogRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNotSuperAdmin_ThrowsForbiddenApiException()
    {
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        Func<Task> act = async () => await _handler.Handle(
            new GetUserActivityLogsQuery(null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenApiException>()
            .WithMessage("*SuperAdmin*");
    }

    [Fact]
    public async Task Handle_WhenNoFilter_ReturnsMappedLogs()
    {
        UserActivityLog first = CreateLog(UserActivityEventType.Login, "1.1.1.1");
        UserActivityLog second = CreateLog(UserActivityEventType.DemoEnter, "2.2.2.2");
        SetupPagedResult(new List<UserActivityLog> { first, second });

        IReadOnlyList<UserActivityLogWeb> result = await _handler.Handle(
            new GetUserActivityLogsQuery(null),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].EventType.Should().Be(UserActivityEventType.Login.ToString());
        result[0].IpAddress.Should().Be("1.1.1.1");
        result[1].EventType.Should().Be(UserActivityEventType.DemoEnter.ToString());

        _activityLogRepoMock.Verify(
            r => r.GetPagedBySearchAsync(
                It.IsAny<Expression<Func<UserActivityLog, bool>>>(),
                It.IsAny<Expression<Func<UserActivityLog, DateTime>>>(),
                true,
                0,
                500,
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<UserActivityLog>, IIncludableQueryable<UserActivityLog, object>>[]>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventTypeFilterProvided_ReturnsFilteredMappedLogs()
    {
        UserActivityLog login = CreateLog(UserActivityEventType.Login, "9.9.9.9");
        SetupPagedResult(new List<UserActivityLog> { login });

        IReadOnlyList<UserActivityLogWeb> result = await _handler.Handle(
            new GetUserActivityLogsQuery(UserActivityEventType.Login),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].EventType.Should().Be(UserActivityEventType.Login.ToString());
    }

    private void SetupPagedResult(List<UserActivityLog> items)
    {
        _activityLogRepoMock
            .Setup(r => r.GetPagedBySearchAsync(
                It.IsAny<Expression<Func<UserActivityLog, bool>>>(),
                It.IsAny<Expression<Func<UserActivityLog, DateTime>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<UserActivityLog>, IIncludableQueryable<UserActivityLog, object>>[]>()))
            .ReturnsAsync(items);
    }

    private static UserActivityLog CreateLog(UserActivityEventType eventType, string ip)
    {
        return new UserActivityLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            IpAddress = ip,
            OccurredAtUtc = DateTime.UtcNow,
            Route = "/home",
            UserId = null,
            AzureAdB2CObjectId = null
        };
    }
}
