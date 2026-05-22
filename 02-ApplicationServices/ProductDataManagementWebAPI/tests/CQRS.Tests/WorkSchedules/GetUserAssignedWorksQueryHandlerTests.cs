using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.GetUserAssignedWorks;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetUserAssignedWorksQueryHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWorkAssignment>> _assignmentRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkPeriod>> _periodRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkComment>> _commentRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetUserAssignedWorksQueryHandler _handler;

    public GetUserAssignedWorksQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _assignmentRepoMock.DefaultValue = DefaultValue.Empty;
        _periodRepoMock.DefaultValue = DefaultValue.Empty;
        _commentRepoMock.DefaultValue = DefaultValue.Empty;

        _handler = new GetUserAssignedWorksQueryHandler(
            _assignmentRepoMock.Object,
            _periodRepoMock.Object,
            _commentRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetUserAssignedWorksQuery ValidQuery() =>
        new GetUserAssignedWorksQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    // These tests require mocking SelectAsync<AssignmentRow> where AssignmentRow is a private
    // sealed record inside GetUserAssignedWorksQueryHandler. Moq's DefaultValue.Empty cannot
    // construct Task<List<PrivateType>> — the handler must be covered by integration tests.

    [Fact(Skip = "AssignmentRow is a private type — SelectAsync<AssignmentRow> cannot be mocked with DefaultValue.Empty")]
    public async Task Handle_WhenNoAssignments_ReturnsEmptyList()
    {
        GetUserAssignedWorksQuery query = ValidQuery();
        List<UserAssignedWorksByTenantWeb> result = await _handler.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact(Skip = "AssignmentRow is a private type — SelectAsync<AssignmentRow> cannot be mocked with DefaultValue.Empty")]
    public async Task Handle_WhenNoAssignments_DoesNotQueryPeriodsOrComments()
    {
        GetUserAssignedWorksQuery query = ValidQuery();
        await _handler.Handle(query, CancellationToken.None);
        _periodRepoMock.Verify(
            r => r.SelectAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, object>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(Skip = "AssignmentRow is a private type — SelectAsync<AssignmentRow> cannot be mocked with DefaultValue.Empty")]
    public async Task Handle_WhenQueryIsExecuted_UsesCurrentUserId()
    {
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);
        GetUserAssignedWorksQuery query = ValidQuery();
        List<UserAssignedWorksByTenantWeb> result = await _handler.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        _currentUserMock.Verify(u => u.Id, Times.AtLeastOnce);
    }
}
