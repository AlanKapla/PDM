using CQRS.Activity.RecordDemoActivity;
using Entities.Enums;
using Entities.Models.Activity;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.Activity;

public sealed class RecordDemoActivityCommandHandlerTests
{
    private readonly Mock<IRepository<UserActivityLog>> _activityLogRepoMock = new();
    private readonly RecordDemoActivityCommandHandler _handler;

    public RecordDemoActivityCommandHandlerTests()
    {
        _handler = new RecordDemoActivityCommandHandler(_activityLogRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsDemoEnterWithoutIdentity()
    {
        // Arrange
        RecordDemoActivityCommand command = new()
        {
            IpAddress = "203.0.113.10",
            Route = "/demo"
        };

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _activityLogRepoMock.Verify(
            r => r.Insert(It.Is<UserActivityLog>(l =>
                l.EventType == UserActivityEventType.DemoEnter
                && l.IpAddress == "203.0.113.10"
                && l.Route == "/demo"
                && l.UserId == null
                && l.AzureAdB2CObjectId == null)),
            Times.Once);
        _activityLogRepoMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
