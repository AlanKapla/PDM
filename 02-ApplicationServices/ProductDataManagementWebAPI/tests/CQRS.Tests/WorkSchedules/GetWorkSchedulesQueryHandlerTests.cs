using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedules;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetWorkSchedulesQueryHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetWorkSchedulesQueryHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();

    public GetWorkSchedulesQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMemberUserInfo>());

        _handler = new GetWorkSchedulesQueryHandler(
            _workScheduleRepoMock.Object,
            _userServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetWorkSchedulesQuery ValidQuery(ResourceScope scope = ResourceScope.All) =>
        new GetWorkSchedulesQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Scope = scope
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenScopeIsAll_ReturnsAllProjectSchedules()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery(ResourceScope.All);

        _workScheduleRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(new List<WorkSchedule>
            {
                new WorkSchedule { Id = Guid.NewGuid(), Name = "Schedule 1", TenantId = query.TenantId, ProjectId = query.ProjectId, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
                new WorkSchedule { Id = Guid.NewGuid(), Name = "Schedule 2", TenantId = query.TenantId, ProjectId = query.ProjectId, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
            });

        // Act
        List<WorkScheduleSummaryWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenScopeIsMine_ReturnsOnlyCurrentUserSchedules()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery(ResourceScope.Mine);

        _workScheduleRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(new List<WorkSchedule>
            {
                new WorkSchedule { Id = Guid.NewGuid(), Name = "My Schedule", TenantId = query.TenantId, ProjectId = query.ProjectId, CreatedByUserId = _currentUserId, CreatedAt = DateTime.UtcNow }
            });

        // Act
        List<WorkScheduleSummaryWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("My Schedule");
    }

    [Fact]
    public async Task Handle_WhenScopeIsShared_ThrowsNotImplementedApiException()
    {
        // Arrange
        GetWorkSchedulesQuery query = ValidQuery(ResourceScope.Shared);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotImplementedApiException>();
    }
}
